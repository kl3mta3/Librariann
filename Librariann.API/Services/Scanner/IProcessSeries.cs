using System.Collections.Generic;
using System.Threading.Tasks;
using Librariann.Models.DTOs.LibrariannPlus.Metadata;
using Librariann.Models.Entities;
using Librariann.Models.Parser;

namespace Librariann.API.Services.Scanner;

public sealed record ProcessSeriesArgs
{
    public required Library Library { get; init; }
    public required int TotalToProcess { get; init; }
    public required int LeftToProcess { get; init; }
    public bool ForceUpdate { get; init; } = false;
}

public interface IProcessSeries
{
    Task<int?> ProcessSeriesAsync(MetadataSettingsDto settings, IList<ParserInfo> parsedInfos, ProcessSeriesArgs args);
}
