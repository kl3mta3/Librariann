using System;
using System.Threading;
using System.Threading.Tasks;
using Librariann.Common.Helpers;
using Librariann.Models.DTOs.LibrariannPlus;
using Librariann.Models.DTOs.LibrariannPlus.Audit;
using Librariann.Models.Entities.History;

namespace Librariann.API.Repositories;

public interface ILibrariannPlusAuditRepository
{
    void Add(LibrariannPlusAuditLog entry);
    Task DeleteOlderThanAsync(DateTime cutoff, CancellationToken ct = default);
    Task<int> GetScrobbleFailureCountAsync(int userId, CancellationToken ct = default);
    Task<PagedList<LibrariannPlusAuditEntryDto>> GetPagedAsync(
        LibrariannPlusAuditFilterDto filter, UserParams userParams, CancellationToken ct = default);
    Task<PagedList<LibrariannPlusAuditEntryDto>> GetMyActivityAsync(
        int userId, LibrariannPlusAuditFilterDto filter, UserParams userParams, CancellationToken ct = default);
    Task<LibrariannPlusAuditStatsDto> GetStatsAsync(CancellationToken ct = default);
    Task<LibrariannPlusMyAuditStatsDto> GetMyStatsAsync(int userId, CancellationToken ct = default);
    Task<LibrariannPlusAuditSeriesInfoDto> GetSeriesInfoAsync(
        int seriesId, int callingUserId, bool isAdmin, CancellationToken ct = default);
    Task MarkAsRetriedAsync(long id, CancellationToken ct = default);

}
