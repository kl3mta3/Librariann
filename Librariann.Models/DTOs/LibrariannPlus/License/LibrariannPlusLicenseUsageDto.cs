using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Librariann.Models.DTOs.LibrariannPlus.License;

public sealed record LibrariannPlusLicenseUsageDto
{
    public DateTime GeneratedAtUtc { get; set; }
    public IReadOnlyList<ApiUsageDto> Stats { get; set; }
}

public sealed record ApiUsageDto
{
    [EnumDataType(typeof(LibrariannPlusApiName))]
    public LibrariannPlusApiName ApiName { get; set; }
    public long LifetimeCount { get; set; }
    public long Last30DaysCount { get; set; }
    public IReadOnlyList<DailyBucketDto> DailyBuckets { get; set; } = [];
}

public sealed record DailyBucketDto
{
    public DateOnly Date { get; set; }
    public long Count { get; set; }
}

public enum LibrariannPlusApiName
{
    CoverRequests   = 1,
    MetadataSync    = 2,
    SeriesMatched   = 3,
    Scrobbles       = 4,
    MalStackImport  = 5,
    WantToRead      = 6,
    Recommendations = 7,
    Reviews = 8,
}