using System;
using System.Collections.Generic;
using Librariann.Models.DTOs.Acquisition;

namespace Librariann.Models.Entities.Acquisition;

public enum IntegrationProviderCategory
{
    Indexer = 1,
    DownloadClient = 2,
    Metadata = 3,
    Tts = 4,
    Notification = 5,
    Authentication = 6,
}

/// <summary>
/// Persisted configuration for a modular provider. Secret properties contain only
/// Data Protection payloads and must never be serialized to an API response.
/// </summary>
public class IntegrationProviderConfiguration
{
    public int Id { get; set; }
    public Guid CredentialKey { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public IntegrationProviderCategory Category { get; set; }
    public string ProviderType { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = string.Empty;
    public bool AllowPrivateNetwork { get; set; }
    public bool IsEnabled { get; set; } = true;
    public string DownloadCategory { get; set; } = "librariann";
    public string RemotePath { get; set; } = string.Empty;
    public string LocalPath { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = [];

    public string ProtectedUsername { get; set; } = string.Empty;
    public string ProtectedPassword { get; set; } = string.Empty;
    public string ProtectedApiKey { get; set; } = string.Empty;

    public DownloadClientKind? DownloadClientKind { get; set; }
    public IndexerProtocol? IndexerProtocol { get; set; }
}
