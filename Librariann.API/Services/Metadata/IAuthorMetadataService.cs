using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Librariann.Models.DTOs.Person;

namespace Librariann.API.Services.Metadata;

public interface IAuthorMetadataService
{
    Task<IReadOnlyCollection<AuthorMetadataCandidateDto>> SearchAsync(string query,
        CancellationToken cancellationToken = default);

    Task<AuthorMetadataDetails?> GetDetailsAsync(string providerKey, string externalId,
        CancellationToken cancellationToken = default);
}

public sealed record AuthorMetadataDetails
{
    public string ProviderKey { get; init; } = string.Empty;
    public string ExternalId { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public IReadOnlyCollection<string> Aliases { get; init; } = [];
    public string Description { get; init; } = string.Empty;
    public string PortraitUrl { get; init; } = string.Empty;
}

