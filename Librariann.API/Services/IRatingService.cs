using System.Threading;
using System.Threading.Tasks;
using Librariann.Models.DTOs;
using Librariann.Models.Entities.User;

namespace Librariann.API.Services;

public interface IRatingService
{
    /// <summary>
    /// Updates the users' rating for a given series
    /// </summary>
    /// <param name="user">Should include ratings</param>
    /// <param name="updateRatingDto"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    Task<bool> UpdateSeriesRating(AppUser user, UpdateRatingDto updateRatingDto, CancellationToken ct = default);

    /// <summary>
    /// Updates the users' rating for a given chapter
    /// </summary>
    /// <param name="user">Should include ratings</param>
    /// <param name="updateRatingDto">chapterId must be set</param>
    /// <param name="ct"></param>
    /// <returns></returns>
    Task<bool> UpdateChapterRating(AppUser user, UpdateRatingDto updateRatingDto, CancellationToken ct = default);
}
