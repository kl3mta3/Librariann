using System;
using System.Collections.Generic;
using Librariann.Models.DTOs.Progress;
using Librariann.Models.Entities.User;
using Microsoft.EntityFrameworkCore;

namespace Librariann.Models.Entities.Progress;

/// <summary>
/// Represents a single day's worth of Reading Sessions
/// </summary>
public class AppUserReadingHistory
{
    public int Id { get; set; }
    public DateTime DateUtc { get; set; }
    /// <summary>
    /// JSON Column
    /// </summary>
    public DailyReadingDataDto Data { get; set; }

    /// <summary>
    /// JSON Column
    /// </summary>
    public IList<ClientInfoData> ClientInfoUsed { get; set; }


    public int AppUserId { get; set; }
    public virtual AppUser AppUser { get; set; }
}
