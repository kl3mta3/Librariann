using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Librariann.Models.DTOs.Reader;

namespace Librariann.Models.DTOs.Downloads;

public sealed record DownloadBookmarkDto
{
    [Required]
    public IEnumerable<BookmarkDto> Bookmarks { get; set; } = default!;
}
