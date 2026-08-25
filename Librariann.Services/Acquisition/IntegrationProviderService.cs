using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using Librariann.API.Database;
using Librariann.API.Services;
using Librariann.API.Services.Acquisition;
using Librariann.Common;
using Librariann.Models.DTOs.Acquisition;
using Librariann.Models.Entities.Acquisition;

namespace Librariann.Services.Acquisition;

public sealed class IntegrationProviderService(
    IUnitOfWork unitOfWork,
    ICredentialProtectionService credentialProtection,
    IIntegrationEndpointValidator endpointValidator) : IIntegrationProviderService
{
    public async Task<IReadOnlyList<IntegrationProviderDto>> GetAllAsync(CancellationToken ct = default) =>
        (await unitOfWork.IntegrationProviderRepository.GetAllAsync(ct)).Select(ToDto).ToList();

    public async Task<IntegrationProviderDto> CreateAsync(UpsertIntegrationProviderDto dto, CancellationToken ct = default)
    {
        var endpoint = await endpointValidator.ValidateAsync(dto.BaseUrl, dto.AllowPrivateNetwork, ct);
        ValidateProviderKind(dto);
        ValidatePathMapping(dto);

        var configuration = new IntegrationProviderConfiguration();
        Apply(configuration, dto, endpoint);
        ApplySecrets(configuration, dto);
        unitOfWork.IntegrationProviderRepository.Add(configuration);
        await unitOfWork.CommitAsync(ct);
        return ToDto(configuration);
    }

    public async Task<IntegrationProviderDto> UpdateAsync(UpsertIntegrationProviderDto dto, CancellationToken ct = default)
    {
        var configuration = await unitOfWork.IntegrationProviderRepository.GetAsync(dto.Id, ct)
                            ?? throw new LibrariannException("integration-provider-does-not-exist");
        var endpoint = await endpointValidator.ValidateAsync(dto.BaseUrl, dto.AllowPrivateNetwork, ct);
        ValidateProviderKind(dto);
        ValidatePathMapping(dto);

        Apply(configuration, dto, endpoint);
        ApplySecrets(configuration, dto);
        unitOfWork.IntegrationProviderRepository.Update(configuration);
        await unitOfWork.CommitAsync(ct);
        return ToDto(configuration);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var configuration = await unitOfWork.IntegrationProviderRepository.GetAsync(id, ct)
                            ?? throw new LibrariannException("integration-provider-does-not-exist");
        unitOfWork.IntegrationProviderRepository.Remove(configuration);
        await unitOfWork.CommitAsync(ct);
    }

    private static void Apply(IntegrationProviderConfiguration configuration, UpsertIntegrationProviderDto dto, Uri endpoint)
    {
        configuration.Name = dto.Name.Trim();
        configuration.Category = dto.Category;
        configuration.ProviderType = dto.ProviderType.Trim();
        configuration.BaseUrl = endpoint.ToString();
        configuration.AllowPrivateNetwork = dto.AllowPrivateNetwork;
        configuration.IsEnabled = dto.IsEnabled;
        configuration.DownloadCategory = string.IsNullOrWhiteSpace(dto.DownloadCategory) ? "librariann" : dto.DownloadCategory.Trim();
        configuration.RemotePath = dto.RemotePath.Trim();
        configuration.LocalPath = string.IsNullOrWhiteSpace(dto.LocalPath) ? string.Empty : Path.GetFullPath(dto.LocalPath.Trim());
        configuration.Tags = dto.Tags.Where(tag => !string.IsNullOrWhiteSpace(tag)).Select(tag => tag.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        configuration.DownloadClientKind = dto.DownloadClientKind;
        configuration.IndexerProtocol = dto.IndexerProtocol;
    }

    private void ApplySecrets(IntegrationProviderConfiguration configuration, UpsertIntegrationProviderDto dto)
    {
        configuration.ProtectedUsername = UpdateSecret(configuration, "username", configuration.ProtectedUsername,
            dto.Username, dto.ClearUsername);
        configuration.ProtectedPassword = UpdateSecret(configuration, "password", configuration.ProtectedPassword,
            dto.Password, dto.ClearPassword);
        configuration.ProtectedApiKey = UpdateSecret(configuration, "api-key", configuration.ProtectedApiKey,
            dto.ApiKey, dto.ClearApiKey);
    }

    private string UpdateSecret(IntegrationProviderConfiguration configuration, string field, string current,
        string? replacement, bool clear)
    {
        if (clear) return string.Empty;
        if (replacement is null) return current;
        if (replacement.Length == 0) return current;
        return credentialProtection.Protect(replacement, IntegrationCredentialScope.For(configuration, field));
    }

    private IntegrationProviderDto ToDto(IntegrationProviderConfiguration configuration) => new()
    {
        Id = configuration.Id,
        Name = configuration.Name,
        Category = configuration.Category,
        ProviderType = configuration.ProviderType,
        BaseUrl = configuration.BaseUrl,
        AllowPrivateNetwork = configuration.AllowPrivateNetwork,
        IsEnabled = configuration.IsEnabled,
        DownloadCategory = configuration.DownloadCategory,
        RemotePath = configuration.RemotePath,
        LocalPath = configuration.LocalPath,
        Tags = configuration.Tags,
        DownloadClientKind = configuration.DownloadClientKind,
        IndexerProtocol = configuration.IndexerProtocol,
        HasUsername = credentialProtection.IsProtected(configuration.ProtectedUsername),
        HasPassword = credentialProtection.IsProtected(configuration.ProtectedPassword),
        HasApiKey = credentialProtection.IsProtected(configuration.ProtectedApiKey),
    };

    private static void ValidateProviderKind(UpsertIntegrationProviderDto dto)
    {
        if (dto.Category == IntegrationProviderCategory.DownloadClient && dto.DownloadClientKind is null)
            throw new LibrariannException("integration-provider-download-client-kind-required");
        if (dto.Category == IntegrationProviderCategory.Indexer && dto.IndexerProtocol is null)
            throw new LibrariannException("integration-provider-indexer-protocol-required");
    }

    private static void ValidatePathMapping(UpsertIntegrationProviderDto dto)
    {
        if (dto.Category != IntegrationProviderCategory.DownloadClient &&
            (!string.IsNullOrWhiteSpace(dto.RemotePath) || !string.IsNullOrWhiteSpace(dto.LocalPath)))
            throw new LibrariannException("path-mapping-download-client-only");
        if (string.IsNullOrWhiteSpace(dto.RemotePath) != string.IsNullOrWhiteSpace(dto.LocalPath))
            throw new LibrariannException("path-mapping-both-paths-required");
        if (string.IsNullOrWhiteSpace(dto.LocalPath)) return;

        string fullPath;
        try
        {
            fullPath = Path.GetFullPath(dto.LocalPath.Trim());
        }
        catch (Exception exception) when (exception is ArgumentException or NotSupportedException or PathTooLongException)
        {
            throw new LibrariannException("path-mapping-local-path-invalid");
        }
        if (!Path.IsPathFullyQualified(fullPath) || string.Equals(fullPath.TrimEnd(Path.DirectorySeparatorChar),
                Path.GetPathRoot(fullPath)?.TrimEnd(Path.DirectorySeparatorChar), StringComparison.OrdinalIgnoreCase))
            throw new LibrariannException("path-mapping-local-path-too-broad");
    }

}
