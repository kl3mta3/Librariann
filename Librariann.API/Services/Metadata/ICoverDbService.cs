using System.Threading;
using System.Threading.Tasks;
using Librariann.Models.Entities;
using Librariann.Models.Entities.Enums;
using Librariann.Models.Entities.Person;
using Librariann.Models.Entities.User;

namespace Librariann.API.Services.Metadata;

public interface ICoverDbService
{
    Task<string> DownloadFaviconAsync(string url, EncodeFormat encodeFormat, CancellationToken ct = default);
    Task<string> DownloadPublisherImageAsync(string publisherName, EncodeFormat encodeFormat, CancellationToken ct = default);
    Task<string?> DownloadPersonImageAsync(Person person, EncodeFormat encodeFormat, CancellationToken ct = default);
    Task<string?> DownloadPersonImageAsync(Person person, EncodeFormat encodeFormat, string url, CancellationToken ct = default);
    Task SetPersonCoverByUrl(Person person, string url, bool fromBase64 = true, bool checkNoImagePlaceholder = false, bool chooseBetterImage = true, CancellationToken ct = default);
    Task SetSeriesCoverByUrl(Series series, string url, bool fromBase64 = true, bool chooseBetterImage = false, CancellationToken ct = default);
    Task SetChapterCoverByUrl(Chapter chapter, string url, bool fromBase64 = true, bool chooseBetterImage = false, CancellationToken ct = default);
    Task SetVolumeCoverByUrl(Volume volume, string url, bool fromBase64 = true, bool chooseBetterImage = false, CancellationToken ct = default);
    /// <summary>
    /// Points a Volume's cover at an existing Chapter's cover image (reuse, no download), mirroring how single-chapter
    /// volumes derive their cover during cover generation.
    /// </summary>
    Task SetVolumeCoverFromChapter(Volume volume, Chapter chapter, CancellationToken ct = default);
    Task SetUserCoverByUrl(int userId, string url, bool fromBase64 = true, bool chooseBetterImage = false, CancellationToken ct = default);
    Task SetUserCoverByUrl(AppUser user, string url, bool fromBase64 = true, bool chooseBetterImage = false, CancellationToken ct = default);
}
