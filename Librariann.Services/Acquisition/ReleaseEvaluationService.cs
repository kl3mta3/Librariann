using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Librariann.API.Services.Acquisition;
using Librariann.Models.DTOs.Acquisition;

namespace Librariann.Services.Acquisition;

public sealed partial class ReleaseEvaluationService : IReleaseEvaluationService
{
    public ReleaseDecision Evaluate(ReleaseCandidate candidate, ReleaseEvaluationContext context)
    {
        var rejections = new List<ReleaseRejection>();
        var score = context.FormatScores.GetValueOrDefault(candidate.Format);

        if (candidate.DownloadUri is null)
            Reject(ReleaseRejectionCode.MissingDownloadUrl, "The indexer did not provide a downloadable URL.");

        if (score <= 0)
            Reject(ReleaseRejectionCode.UnwantedFormat, $"{candidate.Format} is not enabled in the quality profile.");

        if (context.AllowedLanguages.Count > 0 && !context.AllowedLanguages.Any(language =>
                string.Equals(language, candidate.Language, StringComparison.OrdinalIgnoreCase)))
            Reject(ReleaseRejectionCode.WrongLanguage, $"Language '{candidate.Language}' is not allowed.");

        if (context.MinimumSizeBytes.HasValue && candidate.SizeBytes < context.MinimumSizeBytes.Value)
            Reject(ReleaseRejectionCode.BelowMinimumSize, "Release is below the configured minimum size.");

        if (context.MaximumSizeBytes.HasValue && candidate.SizeBytes > context.MaximumSizeBytes.Value)
            Reject(ReleaseRejectionCode.AboveMaximumSize, "Release is above the configured maximum size.");

        if (!IsTextMatch(context.ExpectedTitle, candidate.Title))
            Reject(ReleaseRejectionCode.TitleMismatch, "Release title does not match the monitored title.");

        // Many Torznab/Newznab feeds omit the optional author attribute even when the release name contains it.
        // Match against both normalized fields so author monitoring remains useful without weakening title checks.
        if (!IsTextMatch(context.ExpectedAuthor, $"{candidate.Author} {candidate.Title}"))
            Reject(ReleaseRejectionCode.AuthorMismatch, "Release author does not match the monitored author.");

        if (!IsTextMatch(context.ExpectedEdition, candidate.Edition))
            Reject(ReleaseRejectionCode.WrongEdition, "Release edition does not match the requested edition.");

        if (context.OwnedFormat.HasValue)
        {
            var ownedScore = context.FormatScores.GetValueOrDefault(context.OwnedFormat.Value);
            if (candidate.Format == context.OwnedFormat.Value)
                Reject(ReleaseRejectionCode.AlreadyOwned, $"{candidate.Format} is already owned.");
            else if (!context.UpgradeAllowed)
                Reject(ReleaseRejectionCode.NotAnUpgrade, "Format upgrades are disabled in this quality profile.");
            else if (context.CutoffScore > 0 && ownedScore >= context.CutoffScore)
                Reject(ReleaseRejectionCode.NotAnUpgrade, $"The owned {context.OwnedFormat.Value} copy meets the profile cutoff.");
            else if (score <= ownedScore)
                Reject(ReleaseRejectionCode.NotAnUpgrade, $"{candidate.Format} does not improve on the owned {context.OwnedFormat.Value} copy.");
        }

        if (candidate.IsRetail && context.PreferRetail) score += 10;
        score += Math.Min(candidate.Seeders.GetValueOrDefault(), 100) / 10;
        if (candidate.PublishedAt >= DateTimeOffset.UtcNow.AddDays(-30)) score += 5;

        return new ReleaseDecision(candidate, Math.Max(score, 0), rejections);

        void Reject(ReleaseRejectionCode code, string message) => rejections.Add(new ReleaseRejection(code, message));
    }

    private static bool IsTextMatch(string expected, string actual)
    {
        if (string.IsNullOrWhiteSpace(expected)) return true;
        if (string.IsNullOrWhiteSpace(actual)) return false;

        var expectedNormalized = NormalizeTextRegex().Replace(expected, string.Empty);
        var actualNormalized = NormalizeTextRegex().Replace(actual, string.Empty);
        return actualNormalized.Contains(expectedNormalized, StringComparison.OrdinalIgnoreCase)
               || expectedNormalized.Contains(actualNormalized, StringComparison.OrdinalIgnoreCase);
    }

    [GeneratedRegex("[^a-zA-Z0-9]+", RegexOptions.CultureInvariant)]
    private static partial Regex NormalizeTextRegex();
}
