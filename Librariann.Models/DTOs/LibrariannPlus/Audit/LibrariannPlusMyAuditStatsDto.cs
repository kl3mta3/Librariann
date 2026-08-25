namespace Librariann.Models.DTOs.LibrariannPlus.Audit;

public sealed record LibrariannPlusMyAuditStatsDto
{
    public int Events24H { get; init; }
    public int Failures24H { get; init; }
    public int ScrobbleQueueCount { get; init; }
}
