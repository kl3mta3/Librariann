using System.Collections.Generic;

namespace Librariann.Models.DTOs.Downloads;

public record BulkChapterSizeRequest(List<int> ChapterIds);
public record BulkVolumeSizeRequest(List<int> VolumeIds);
public record BulkSeriesSizeRequest(List<int> SeriesIds);
public record BulkReadingListSizeRequest(List<int> ReadingListIds);
