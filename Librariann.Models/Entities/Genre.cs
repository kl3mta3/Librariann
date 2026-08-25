using System.Collections.Generic;
using Librariann.Models.Entities.Interfaces;
using Librariann.Models.Entities.Metadata;
using Microsoft.EntityFrameworkCore;

namespace Librariann.Models.Entities;

[Index(nameof(NormalizedTitle), IsUnique = true)]
public class Genre : ITag
{
    public int Id { get; set; }
    public string Title { get; set; } = null!;
    public string NormalizedTitle { get; set; } = null!;

    public ICollection<SeriesMetadata> SeriesMetadatas { get; set; } = null!;
    public ICollection<Chapter> Chapters { get; set; } = null!;
}
