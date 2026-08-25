using System;
using System.ComponentModel;
using Librariann.Models.Entities.Enums;
using System.ComponentModel.DataAnnotations;

namespace Librariann.Models.DTOs.LibrariannPlus;
#nullable enable

public enum LibrariannPlusProviderHealthStatus
{
    [Description("Unknown")]
    Unknown = 0,
    [Description("Operational")]
    Operational = 1,
    [Description("Degraded")]
    Degraded = 2,
    [Description("Down")]
    Down = 3,
}

public enum LibrariannPlusProviderHealthIncidentType
{
    [Description("Degraded")]
    Degraded = 1,
    [Description("Down")]
    Down = 2,
}

public sealed record LibrariannPlusProviderHealthSnapshotDto
{
    [EnumDataType(typeof(ScrobbleProvider))]
    public ScrobbleProvider Provider { get; set; }
    public double AvgLatencyMs { get; set; }
    [EnumDataType(typeof(LibrariannPlusProviderHealthStatus))]
    public LibrariannPlusProviderHealthStatus Status { get; set; }
    public LibrariannPlusProviderIncidentDto? LastIncident { get; set; }
}

public sealed record LibrariannPlusProviderIncidentDto
{
    public DateTime StartedAtUtc { get; set; }
    public DateTime? EndedAtUtc { get; set; }
    [EnumDataType(typeof(LibrariannPlusProviderHealthIncidentType))]
    public LibrariannPlusProviderHealthIncidentType Type { get; set; }
}