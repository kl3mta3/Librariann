using System;
using System.Buffers.Binary;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Librariann.Services.Security;

/// <summary>
/// Streaming, authenticated encryption for portable Librariann backup archives.
/// Each chunk is independently authenticated and a final authenticated record prevents
/// silent truncation. The format is intentionally versioned for a future restore tool.
/// </summary>
public static class BackupArchiveEncryption
{
    private static readonly byte[] Magic = Encoding.ASCII.GetBytes("LIBRARIANN-BKP01");
    private const int Version = 1;
    private const int SaltSize = 16;
    private const int NonceSize = 12;
    private const int TagSize = 16;
    private const int KeySize = 32;
    private const int ChunkSize = 1024 * 1024;
    private const int Iterations = 600_000;
    private const int HeaderSize = 16 + sizeof(int) * 3 + SaltSize;

    public static async Task EncryptAsync(string inputPath, string outputPath, string passphrase,
        CancellationToken ct = default)
    {
        ValidatePassphrase(passphrase);

        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var header = CreateHeader(salt);
        var key = Rfc2898DeriveBytes.Pbkdf2(passphrase, salt, Iterations, HashAlgorithmName.SHA256, KeySize);

        try
        {
            await using var input = new FileStream(inputPath, FileMode.Open, FileAccess.Read, FileShare.Read,
                ChunkSize, FileOptions.Asynchronous | FileOptions.SequentialScan);
            await using var output = new FileStream(outputPath, FileMode.CreateNew, FileAccess.Write, FileShare.None,
                ChunkSize, FileOptions.Asynchronous | FileOptions.SequentialScan);

            await output.WriteAsync(header, ct);

            using var aes = new AesGcm(key, TagSize);
            var plaintext = new byte[ChunkSize];
            long chunkIndex = 0;

            while (true)
            {
                var length = await ReadChunkAsync(input, plaintext, ct);
                if (length == 0) break;

                await WriteRecordAsync(output, aes, header, chunkIndex++, plaintext.AsMemory(0, length), ct);
            }

            await WriteRecordAsync(output, aes, header, chunkIndex, ReadOnlyMemory<byte>.Empty, ct);
            await output.FlushAsync(ct);
        }
        catch
        {
            TryDelete(outputPath);
            throw;
        }
        finally
        {
            CryptographicOperations.ZeroMemory(key);
        }
    }

    public static async Task DecryptAsync(string inputPath, string outputPath, string passphrase,
        CancellationToken ct = default)
    {
        ValidatePassphrase(passphrase);

        await using var input = new FileStream(inputPath, FileMode.Open, FileAccess.Read, FileShare.Read,
            ChunkSize, FileOptions.Asynchronous | FileOptions.SequentialScan);
        var header = new byte[HeaderSize];
        await ReadExactlyAsync(input, header, ct);
        var (salt, chunkSize, iterations) = ParseHeader(header);
        var key = Rfc2898DeriveBytes.Pbkdf2(passphrase, salt, iterations, HashAlgorithmName.SHA256, KeySize);

        try
        {
            await using var output = new FileStream(outputPath, FileMode.CreateNew, FileAccess.Write, FileShare.None,
                chunkSize, FileOptions.Asynchronous | FileOptions.SequentialScan);
            using var aes = new AesGcm(key, TagSize);
            long chunkIndex = 0;

            while (true)
            {
                var lengthBytes = new byte[sizeof(int)];
                await ReadExactlyAsync(input, lengthBytes, ct);
                var length = BinaryPrimitives.ReadInt32LittleEndian(lengthBytes);
                if (length < 0 || length > chunkSize) throw new InvalidDataException("Invalid encrypted backup record length");

                var nonce = new byte[NonceSize];
                var ciphertext = new byte[length];
                var tag = new byte[TagSize];
                await ReadExactlyAsync(input, nonce, ct);
                await ReadExactlyAsync(input, ciphertext, ct);
                await ReadExactlyAsync(input, tag, ct);

                var plaintext = new byte[length];
                aes.Decrypt(nonce, ciphertext, tag, plaintext, CreateAssociatedData(header, chunkIndex, length));

                if (length == 0)
                {
                    if (input.Position != input.Length) throw new InvalidDataException("Encrypted backup contains trailing data");
                    break;
                }

                await output.WriteAsync(plaintext, ct);
                CryptographicOperations.ZeroMemory(plaintext);
                chunkIndex++;
            }

            await output.FlushAsync(ct);
        }
        catch
        {
            TryDelete(outputPath);
            throw;
        }
        finally
        {
            CryptographicOperations.ZeroMemory(key);
        }
    }

    private static byte[] CreateHeader(byte[] salt)
    {
        var header = new byte[HeaderSize];
        Magic.CopyTo(header, 0);
        BinaryPrimitives.WriteInt32LittleEndian(header.AsSpan(16), Version);
        BinaryPrimitives.WriteInt32LittleEndian(header.AsSpan(20), Iterations);
        BinaryPrimitives.WriteInt32LittleEndian(header.AsSpan(24), ChunkSize);
        salt.CopyTo(header, 28);
        return header;
    }

    private static (byte[] Salt, int ChunkSize, int Iterations) ParseHeader(byte[] header)
    {
        if (!header.AsSpan(0, Magic.Length).SequenceEqual(Magic))
            throw new InvalidDataException("Not a Librariann encrypted backup");

        var version = BinaryPrimitives.ReadInt32LittleEndian(header.AsSpan(16));
        var iterations = BinaryPrimitives.ReadInt32LittleEndian(header.AsSpan(20));
        var chunkSize = BinaryPrimitives.ReadInt32LittleEndian(header.AsSpan(24));
        if (version != Version) throw new InvalidDataException($"Unsupported encrypted backup version {version}");
        if (iterations is < 100_000 or > 10_000_000) throw new InvalidDataException("Invalid key derivation parameters");
        if (chunkSize is < 64 * 1024 or > 16 * 1024 * 1024) throw new InvalidDataException("Invalid chunk size");

        return (header.AsSpan(28, SaltSize).ToArray(), chunkSize, iterations);
    }

    private static async Task WriteRecordAsync(Stream output, AesGcm aes, byte[] header, long chunkIndex,
        ReadOnlyMemory<byte> plaintext, CancellationToken ct)
    {
        var nonce = RandomNumberGenerator.GetBytes(NonceSize);
        var ciphertext = new byte[plaintext.Length];
        var tag = new byte[TagSize];
        aes.Encrypt(nonce, plaintext.Span, ciphertext, tag,
            CreateAssociatedData(header, chunkIndex, plaintext.Length));

        var lengthBytes = new byte[sizeof(int)];
        BinaryPrimitives.WriteInt32LittleEndian(lengthBytes, plaintext.Length);
        await output.WriteAsync(lengthBytes, ct);
        await output.WriteAsync(nonce, ct);
        await output.WriteAsync(ciphertext, ct);
        await output.WriteAsync(tag, ct);
    }

    private static byte[] CreateAssociatedData(byte[] header, long chunkIndex, int length)
    {
        var data = new byte[header.Length + sizeof(long) + sizeof(int)];
        header.CopyTo(data, 0);
        BinaryPrimitives.WriteInt64LittleEndian(data.AsSpan(header.Length), chunkIndex);
        BinaryPrimitives.WriteInt32LittleEndian(data.AsSpan(header.Length + sizeof(long)), length);
        return data;
    }

    private static async Task<int> ReadChunkAsync(Stream input, byte[] buffer, CancellationToken ct)
    {
        var total = 0;
        while (total < buffer.Length)
        {
            var read = await input.ReadAsync(buffer.AsMemory(total), ct);
            if (read == 0) break;
            total += read;
        }
        return total;
    }

    private static async Task ReadExactlyAsync(Stream input, Memory<byte> buffer, CancellationToken ct)
    {
        try
        {
            await input.ReadExactlyAsync(buffer, ct);
        }
        catch (EndOfStreamException ex)
        {
            throw new InvalidDataException("Encrypted backup is truncated", ex);
        }
    }

    private static void ValidatePassphrase(string passphrase)
    {
        if (string.IsNullOrWhiteSpace(passphrase) || passphrase.Length < 16)
            throw new ArgumentException("Backup passphrase must contain at least 16 characters", nameof(passphrase));
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path)) File.Delete(path);
        }
        catch
        {
            // Preserve the original encryption/decryption failure.
        }
    }
}
