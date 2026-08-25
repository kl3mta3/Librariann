using System.Collections.Generic;
using Librariann.Models.Entities.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Librariann.Models.Entities.ReadingLists;

/// <summary>
/// Represents a user-defined string to tag Reading Lists
/// </summary>
[Index(nameof(NormalizedTitle), IsUnique = true)]
public class ReadingListTag : ITag
{
    public int Id { get; set; }
    public string Title { get; set; } = null!;
    public string NormalizedTitle { get; set; } = null!;

    public ICollection<ReadingList> ReadingLists { get; set; }
}
