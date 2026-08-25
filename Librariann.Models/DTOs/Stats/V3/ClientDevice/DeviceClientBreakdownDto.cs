using System.Collections.Generic;
using Librariann.Models.DTOs.Statistics;
using Librariann.Models.Entities.Enums;

namespace Librariann.Models.DTOs.Stats.V3.ClientDevice;

public sealed record DeviceClientBreakdownDto
{
    public IList<StatCount<ClientDeviceType>> Records { get; set; }
    public int TotalCount { get; set; }
}
