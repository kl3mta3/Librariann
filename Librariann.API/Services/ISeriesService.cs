using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Librariann.Common.Helpers;
using Librariann.Models.DTOs;
using Librariann.Models.DTOs.Filtering.v2;
using Librariann.Models.DTOs.SeriesDetail;

namespace Librariann.API.Services;

public interface ISeriesService
{
    Task<SeriesDetailDto> GetSeriesDetail(int seriesId, int userId, CancellationToken ct = default);
    Task<bool> UpdateSeriesMetadata(UpdateSeriesMetadataDto updateSeriesMetadataDto, CancellationToken ct = default);
    Task<bool> DeleteMultipleSeries(IList<int> seriesIds, CancellationToken ct = default);
    Task<bool> UpdateRelatedSeries(UpdateRelatedSeriesDto dto, CancellationToken ct = default);
    Task<RelatedSeriesDto> GetRelatedSeries(int userId, int seriesId, CancellationToken ct = default);
    Task<NextExpectedChapterDto> GetEstimatedChapterCreationDate(int seriesId, int userId, CancellationToken ct = default);
    Task<PagedList<SeriesDto>> GetCurrentlyReading(int userId, int requestingUserId, UserParams userParams, CancellationToken ct = default);
    Task<List<SeriesFilterStatementDto>> GetProfilePrivacyStatements(int userId, int requestingUserId, CancellationToken ct = default);
}
