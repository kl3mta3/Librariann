using System.Collections.Generic;

namespace Librariann.Models.DTOs.ReadingLists.CBL.Import;

public class CblRepoImportRequestDto
{
    public IList<CblRepoItemDto> Items { get; set; } = [];
}
