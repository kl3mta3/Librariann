using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Librariann.Models.Entities.Acquisition;

namespace Librariann.Models.DTOs.Acquisition;

public sealed record IntegrationProviderDto
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public IntegrationProviderCategory Category { get; init; }
    public string ProviderType { get; init; } = string.Empty;
    public string BaseUrl { get; init; } = string.Empty;
    public bool AllowPrivateNetwork { get; init; }
    public bool IsEnabled { get; init; }
    public string DownloadCategory { get; init; } = string.Empty;
    public string RemotePath { get; init; } = string.Empty;
    public string LocalPath { get; init; } = string.Empty;
    public IReadOnlyCollection<string> Tags { get; init; } = new List<string>();
    public DownloadClientKind? DownloadClientKind { get; init; }
    public IndexerProtocol? IndexerProtocol { get; init; }
    public bool HasUsername { get; init; }
    public bool HasPassword { get; init; }
    public bool HasApiKey { get; init; }
}

public sealed record UpsertIntegrationProviderDto
{
    public int Id { get; init; }

    [Required, StringLength(100, MinimumLength = 1)]
    public string Name { get; init; } = string.Empty;

    [EnumDataType(typeof(IntegrationProviderCategory))]
    public IntegrationProviderCategory Category { get; init; }

    [Required, StringLength(100, MinimumLength = 1)]
    public string ProviderType { get; init; } = string.Empty;

    [Required, Url, StringLength(2048)]
    public string BaseUrl { get; init; } = string.Empty;

    public bool AllowPrivateNetwork { get; init; }
    public bool IsEnabled { get; init; } = true;
    [StringLength(100)] public string DownloadCategory { get; init; } = "librariann";
    [StringLength(2048)] public string RemotePath { get; init; } = string.Empty;
    [StringLength(2048)] public string LocalPath { get; init; } = string.Empty;
    public List<string> Tags { get; init; } = [];
    public DownloadClientKind? DownloadClientKind { get; init; }
    public IndexerProtocol? IndexerProtocol { get; init; }

    // Write-only inputs. They are intentionally absent from IntegrationProviderDto.
    [StringLength(512)] public string? Username { get; init; }
    [StringLength(4096)] public string? Password { get; init; }
    [StringLength(4096)] public string? ApiKey { get; init; }
    public bool ClearUsername { get; init; }
    public bool ClearPassword { get; init; }
    public bool ClearApiKey { get; init; }
}
