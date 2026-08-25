using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Librariann.Services.Metadata.Providers;

internal static class MetadataProviderResponseReader
{
    private const int MaximumResponseBytes = 4 * 1024 * 1024;

    public static async Task<byte[]> ReadAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.Content.Headers.ContentLength is > MaximumResponseBytes)
            throw new IOException("Metadata provider response exceeded the allowed size.");
        await using var input = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var output = new MemoryStream();
        var buffer = new byte[81920];
        while (true)
        {
            var read = await input.ReadAsync(buffer, cancellationToken);
            if (read == 0) break;
            if (output.Length + read > MaximumResponseBytes)
                throw new IOException("Metadata provider response exceeded the allowed size.");
            output.Write(buffer, 0, read);
        }
        return output.ToArray();
    }
}
