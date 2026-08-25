using System.Collections.Generic;
using Librariann.Models.Entities.Acquisition;

namespace Librariann.Models.DTOs.Metadata;

public sealed record MetadataCatalogRequest(
    MonitoringTargetKind Kind,
    LibrariannMediaType MediaType,
    string ExternalItemId,
    string Title,
    string Author);

public sealed record MetadataCatalogItem(
    string ProviderKey,
    string ExternalItemId,
    string Title,
    string Author,
    string Series,
    string Sequence,
    int? PublicationYear);

