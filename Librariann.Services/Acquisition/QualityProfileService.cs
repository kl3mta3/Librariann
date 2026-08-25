using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Librariann.API.Database;
using Librariann.API.Services.Acquisition;
using Librariann.Common;
using Librariann.Models.DTOs.Acquisition;
using Librariann.Models.DTOs.Metadata;
using Librariann.Models.Entities.Acquisition;

namespace Librariann.Services.Acquisition;

public sealed class QualityProfileService(IUnitOfWork unitOfWork) : IQualityProfileService
{
    private static readonly SemaphoreSlim DefaultProfileLock = new(1, 1);

    public async Task<IReadOnlyCollection<QualityProfileDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var profiles = await unitOfWork.QualityProfileRepository.GetAllAsync(cancellationToken);
        if (profiles.Count > 0) return profiles.Select(ToDto).ToArray();

        await DefaultProfileLock.WaitAsync(cancellationToken);
        try
        {
            // A second request may have initialized the profiles while this request waited.
            profiles = await unitOfWork.QualityProfileRepository.GetAllAsync(cancellationToken);
            if (profiles.Count > 0) return profiles.Select(ToDto).ToArray();

            var defaults = DefaultProfiles();
            foreach (var profile in defaults) unitOfWork.QualityProfileRepository.Add(profile);
            await unitOfWork.CommitAsync(cancellationToken);
            return defaults.Select(ToDto).ToArray();
        }
        finally
        {
            DefaultProfileLock.Release();
        }
    }

    public async Task<QualityProfileDto> UpsertAsync(UpsertQualityProfileRequest request,
        CancellationToken cancellationToken = default)
    {
        Validate(request);
        var repository = unitOfWork.QualityProfileRepository;
        var duplicate = (await repository.GetAllAsync(cancellationToken)).Any(profile => profile.Id != request.Id &&
            string.Equals(profile.Name, request.Name.Trim(), StringComparison.OrdinalIgnoreCase));
        if (duplicate) throw new LibrariannException("quality-profile-name-already-exists");

        QualityProfile profile;
        if (request.Id > 0)
        {
            profile = await repository.GetAsync(request.Id, cancellationToken)
                      ?? throw new LibrariannException("quality-profile-does-not-exist");
        }
        else
        {
            profile = new QualityProfile();
            repository.Add(profile);
        }
        profile.Name = request.Name.Trim();
        profile.MediaType = request.MediaType;
        profile.Language = request.Language.Trim();
        profile.UpgradeAllowed = request.UpgradeAllowed;
        profile.PreferRetail = request.PreferRetail;
        profile.CutoffFormat = request.CutoffFormat;
        profile.MinimumSizeBytes = request.MinimumSizeBytes;
        profile.MaximumSizeBytes = request.MaximumSizeBytes;
        profile.FormatScores = new Dictionary<AcquisitionMediaFormat, int>(request.FormatScores);
        profile.UpdatedAtUtc = DateTime.UtcNow;
        await unitOfWork.CommitAsync(cancellationToken);
        return ToDto(profile);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var profile = await unitOfWork.QualityProfileRepository.GetAsync(id, cancellationToken)
                      ?? throw new LibrariannException("quality-profile-does-not-exist");
        unitOfWork.QualityProfileRepository.Remove(profile);
        await unitOfWork.CommitAsync(cancellationToken);
    }

    private static void Validate(UpsertQualityProfileRequest request)
    {
        if (request.FormatScores.Count == 0 || request.FormatScores.Any(pair => pair.Key == AcquisitionMediaFormat.Unknown ||
                pair.Value is < 0 or > 100))
            throw new LibrariannException("quality-profile-format-scores-invalid");
        var allowed = AllowedFormats(request.MediaType);
        if (request.FormatScores.Keys.Any(format => !allowed.Contains(format)))
            throw new LibrariannException("quality-profile-format-not-valid-for-media-type");
        if (!request.FormatScores.TryGetValue(request.CutoffFormat, out var cutoffScore) || cutoffScore <= 0)
            throw new LibrariannException("quality-profile-cutoff-must-be-enabled");
        if (request.MinimumSizeBytes.HasValue && request.MaximumSizeBytes.HasValue &&
            request.MinimumSizeBytes > request.MaximumSizeBytes)
            throw new LibrariannException("quality-profile-size-range-invalid");
    }

    private static IReadOnlySet<AcquisitionMediaFormat> AllowedFormats(LibrariannMediaType mediaType) => mediaType switch
    {
        LibrariannMediaType.Book => new HashSet<AcquisitionMediaFormat>
            {AcquisitionMediaFormat.Epub, AcquisitionMediaFormat.Azw3, AcquisitionMediaFormat.Mobi, AcquisitionMediaFormat.Pdf},
        LibrariannMediaType.Comic or LibrariannMediaType.Manga => new HashSet<AcquisitionMediaFormat>
            {AcquisitionMediaFormat.Cbz, AcquisitionMediaFormat.Cbr, AcquisitionMediaFormat.Cb7, AcquisitionMediaFormat.Pdf},
        _ => new HashSet<AcquisitionMediaFormat>(),
    };

    private static QualityProfile[] DefaultProfiles() =>
    [
        new()
        {
            Name = "Books - EPUB preferred",
            MediaType = LibrariannMediaType.Book,
            Language = "English",
            CutoffFormat = AcquisitionMediaFormat.Epub,
            FormatScores = new Dictionary<AcquisitionMediaFormat, int>
            {
                [AcquisitionMediaFormat.Epub] = 100,
                [AcquisitionMediaFormat.Azw3] = 90,
                [AcquisitionMediaFormat.Mobi] = 70,
                [AcquisitionMediaFormat.Pdf] = 50,
            },
        },
        new()
        {
            Name = "Comics - CBZ preferred",
            MediaType = LibrariannMediaType.Comic,
            Language = "English",
            CutoffFormat = AcquisitionMediaFormat.Cbz,
            FormatScores = new Dictionary<AcquisitionMediaFormat, int>
            {
                [AcquisitionMediaFormat.Cbz] = 100,
                [AcquisitionMediaFormat.Cbr] = 80,
                [AcquisitionMediaFormat.Cb7] = 75,
                [AcquisitionMediaFormat.Pdf] = 40,
            },
        },
        new()
        {
            Name = "Manga - CBZ preferred",
            MediaType = LibrariannMediaType.Manga,
            Language = "English",
            CutoffFormat = AcquisitionMediaFormat.Cbz,
            FormatScores = new Dictionary<AcquisitionMediaFormat, int>
            {
                [AcquisitionMediaFormat.Cbz] = 100,
                [AcquisitionMediaFormat.Cbr] = 80,
                [AcquisitionMediaFormat.Cb7] = 75,
                [AcquisitionMediaFormat.Pdf] = 40,
            },
        },
    ];

    private static QualityProfileDto ToDto(QualityProfile profile) => new()
    {
        Id = profile.Id,
        Name = profile.Name,
        MediaType = profile.MediaType,
        Language = profile.Language,
        UpgradeAllowed = profile.UpgradeAllowed,
        PreferRetail = profile.PreferRetail,
        CutoffFormat = profile.CutoffFormat,
        MinimumSizeBytes = profile.MinimumSizeBytes,
        MaximumSizeBytes = profile.MaximumSizeBytes,
        FormatScores = profile.FormatScores,
    };
}
