using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Librariann.API.Database;
using Librariann.API.Repositories;
using Librariann.API.Services.Metadata;
using Librariann.Common;
using Librariann.Models.DTOs.Metadata;
using Librariann.Models.Entities.Metadata;

namespace Librariann.Services.Metadata;

public sealed class MetadataProvenanceService(IUnitOfWork unitOfWork) : IMetadataProvenanceService
{
    public async Task<IReadOnlyCollection<MetadataProvenanceDto>> GetAllAsync(MetadataEntityType entityType, int entityId,
        CancellationToken cancellationToken = default) =>
        (await unitOfWork.MetadataFieldProvenanceRepository.GetAllAsync(entityType, entityId, cancellationToken))
        .Select(ToDto)
        .ToArray();

    public async Task<MetadataRefreshPermission> CanRefreshAsync(MetadataEntityType entityType, int entityId,
        MetadataFieldKey field, string providerKey, CancellationToken cancellationToken = default)
    {
        var provenance = await unitOfWork.MetadataFieldProvenanceRepository.GetAsync(entityType, entityId, field,
            cancellationToken);
        if (provenance is null) return new MetadataRefreshPermission(true, "No source currently owns this field.");
        if (provenance.IsUserOverride)
            return new MetadataRefreshPermission(false, "This field is protected by a user override.");
        if (string.Equals(provenance.ProviderKey, providerKey.Trim(), StringComparison.OrdinalIgnoreCase))
            return new MetadataRefreshPermission(true, "The same provider may refresh its field.");
        return new MetadataRefreshPermission(false, $"This field is currently owned by {provenance.ProviderKey}.");
    }

    public async Task<MetadataProvenanceDto> RecordAsync(RecordMetadataProvenanceRequest request,
        CancellationToken cancellationToken = default)
    {
        await StageAsync(request, cancellationToken);
        await unitOfWork.CommitAsync(cancellationToken);
        var provenance = await unitOfWork.MetadataFieldProvenanceRepository.GetAsync(request.EntityType,
            request.EntityId, request.Field, cancellationToken);
        return ToDto(provenance!);
    }

    public async Task StageAsync(RecordMetadataProvenanceRequest request,
        CancellationToken cancellationToken = default)
    {
        await EnsureEntityExistsAsync(request.EntityType, request.EntityId, cancellationToken);
        var repository = unitOfWork.MetadataFieldProvenanceRepository;
        var provenance = await repository.GetAsync(request.EntityType, request.EntityId, request.Field, cancellationToken);
        if (provenance?.IsUserOverride == true && !request.IsUserOverride)
            throw new LibrariannException("metadata-field-is-protected-by-user-override");

        var providerKey = request.IsUserOverride ? "user" : request.ProviderKey.Trim().ToLowerInvariant();
        if (provenance is null)
        {
            provenance = new MetadataFieldProvenance
            {
                EntityType = request.EntityType,
                EntityId = request.EntityId,
                Field = request.Field,
            };
            repository.Add(provenance);
        }
        provenance.ProviderKey = providerKey;
        provenance.ProviderItemId = request.ProviderItemId.Trim();
        provenance.ValueHash = Hash(request.CanonicalValue);
        provenance.IsUserOverride = request.IsUserOverride;
        provenance.UpdatedAtUtc = DateTime.UtcNow;
    }

    private async Task EnsureEntityExistsAsync(MetadataEntityType entityType, int entityId, CancellationToken cancellationToken)
    {
        var exists = entityType switch
        {
            MetadataEntityType.Series => await unitOfWork.SeriesRepository.GetSeriesByIdAsync(entityId,
                SeriesIncludes.None, cancellationToken) is not null,
            MetadataEntityType.Volume => await unitOfWork.VolumeRepository.GetVolumeByIdAsync(entityId,
                VolumeIncludes.None, cancellationToken) is not null,
            MetadataEntityType.Chapter => await unitOfWork.ChapterRepository.GetChapterAsync(entityId,
                ChapterIncludes.None, cancellationToken) is not null,
            _ => false,
        };
        if (!exists) throw new LibrariannException("metadata-target-does-not-exist");
    }

    private static string Hash(string value) => Convert.ToHexString(SHA256.HashData(
        Encoding.UTF8.GetBytes(value.Normalize(NormalizationForm.FormC))));

    private static MetadataProvenanceDto ToDto(MetadataFieldProvenance provenance) => new(provenance.Id,
        provenance.EntityType, provenance.EntityId, provenance.Field, provenance.ProviderKey,
        provenance.ProviderItemId, provenance.IsUserOverride, provenance.UpdatedAtUtc);
}
