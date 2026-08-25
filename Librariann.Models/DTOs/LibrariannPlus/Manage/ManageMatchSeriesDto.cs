using System;

namespace Librariann.Models.DTOs.LibrariannPlus.Manage;

public sealed record ManageMatchSeriesDto
{
    public SeriesDto Series { get; set; }
    public bool IsMatched { get; set; }
    public DateTime ValidUntilUtc { get; set; }
}
