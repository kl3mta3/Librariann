using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Librariann.API.Database;
using Librariann.API.Services.Metadata;
using Librariann.Common;
using Librariann.Models.DTOs.Metadata;
using Librariann.Models.Entities.Acquisition;
using Librariann.Models.Entities.Enums;

namespace Librariann.Services.Metadata;

public sealed class MetadataLookupService(IUnitOfWork unitOfWork, IMetadataProviderFactory providerFactory,
    IMetadataApplyTokenStore tokenStore)
    : IMetadataLookupService
{
    private static readonly IReadOnlyDictionary<LibrariannMediaType, IReadOnlyDictionary<string, int>> ProviderPriorities =
        new Dictionary<LibrariannMediaType, IReadOnlyDictionary<string, int>>
        {
            [LibrariannMediaType.Book] = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
            {
                ["open-library"] = 100,
                ["google-books"] = 90,
                ["hardcover"] = 80,
            },
            [LibrariannMediaType.Comic] = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
            {
                ["comic-vine"] = 100,
                ["open-library"] = 40,
            },
            [LibrariannMediaType.Manga] = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
            {
                ["anilist"] = 100,
                ["mangadex"] = 90,
                ["open-library"] = 30,
            },
        };

    public async Task<MetadataLookupResponse> SearchAsync(int userId, MetadataLookupRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Title) && string.IsNullOrWhiteSpace(request.Author) &&
            string.IsNullOrWhiteSpace(request.Isbn) && request.Identifiers.Count == 0)
            throw new LibrariannException("metadata-search-requires-title-author-isbn-or-identifier");

        // Provider filters are security-relevant. Never trust the browser's adult-content flag on its own:
        // unrestricted/adult profiles may opt in, while every restricted profile fails closed.
        var user = await unitOfWork.UserRepository.GetUserByIdAsync(userId, ct: cancellationToken);
        var mayViewAdult = user?.AgeRestriction is AgeRating.NotApplicable or >= AgeRating.AdultsOnly;
        request = request with {IncludeAdult = request.IncludeAdult && mayViewAdult};

        var configurations = (await unitOfWork.IntegrationProviderRepository.GetAllAsync(cancellationToken))
            .Where(configuration => configuration.IsEnabled && configuration.Category == IntegrationProviderCategory.Metadata)
            .ToArray();
        var searches = configurations.Select(configuration => SearchProviderAsync(configuration, request, cancellationToken));
        var providerResults = await Task.WhenAll(searches);

        var decisions = providerResults.SelectMany(result => result.Candidates)
            .Where(candidate => request.IncludeAdult || !candidate.IsAdult)
            .Select(candidate => Evaluate(request, candidate))
            .OrderByDescending(decision => decision.Score)
            .ThenByDescending(decision => Priority(request.MediaType, decision.Candidate.ProviderKey))
            .ThenBy(decision => decision.Candidate.Title, StringComparer.OrdinalIgnoreCase)
            .Select(decision => decision with {ApplyToken = tokenStore.Issue(userId, decision.Candidate)})
            .ToArray();
        return new MetadataLookupResponse(decisions,
            providerResults.Where(result => result.Failure is not null).Select(result => result.Failure!).ToArray());
    }

    private async Task<ProviderSearchResult> SearchProviderAsync(IntegrationProviderConfiguration configuration,
        MetadataLookupRequest request, CancellationToken cancellationToken)
    {
        try
        {
            using var provider = providerFactory.Create(configuration);
            if (!provider.SupportedMediaTypes.Contains(request.MediaType)) return new ProviderSearchResult([], null);
            return new ProviderSearchResult(await provider.SearchAsync(request, cancellationToken), null);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            return new ProviderSearchResult([], new MetadataProviderFailure(configuration.ProviderType,
                configuration.Name, "The metadata provider search failed."));
        }
    }

    private static MetadataMatchDecision Evaluate(MetadataLookupRequest request, NormalizedMetadataCandidate candidate)
    {
        var score = Priority(request.MediaType, candidate.ProviderKey) / 20;
        var reasons = new List<string>();

        var requestIdentifiers = request.Identifiers
            .Where(pair => !string.IsNullOrWhiteSpace(pair.Key) && !string.IsNullOrWhiteSpace(pair.Value));
        if (requestIdentifiers.Any(pair => candidate.Identifiers.TryGetValue(pair.Key, out var value) &&
                                           Normalize(value) == Normalize(pair.Value)))
        {
            score += 80;
            reasons.Add("Exact identifier");
        }

        var isbn = Normalize(request.Isbn);
        if (isbn.Length > 0 && candidate.Isbns.Any(value => Normalize(value) == isbn))
        {
            score += 70;
            reasons.Add("Exact ISBN");
        }

        var title = Normalize(request.Title);
        if (title.Length > 0)
        {
            var candidateTitles = candidate.AlternateTitles.Prepend(candidate.Title).Select(Normalize).ToArray();
            if (candidateTitles.Contains(title))
            {
                score += 40;
                reasons.Add("Exact title");
            }
            else
            {
                var similarity = candidateTitles.Select(value => TokenSimilarity(title, value)).DefaultIfEmpty().Max();
                if (similarity >= 0.5)
                {
                    score += (int) Math.Round(similarity * 30);
                    reasons.Add("Similar title");
                }
            }
        }

        var author = Normalize(request.Author);
        if (author.Length > 0 && candidate.Authors.Any(value => Normalize(value) == author))
        {
            score += 20;
            reasons.Add("Exact author");
        }
        if (!string.IsNullOrWhiteSpace(request.Series) && Normalize(request.Series) == Normalize(candidate.Series))
        {
            score += 10;
            reasons.Add("Exact series");
        }
        if (request.PublicationYear.HasValue && request.PublicationYear == candidate.PublicationYear)
        {
            score += 5;
            reasons.Add("Publication year");
        }
        if (!string.IsNullOrWhiteSpace(request.Language) && candidate.Languages.Any(language =>
                NormalizeLanguage(language) == NormalizeLanguage(request.Language)))
        {
            score += 5;
            reasons.Add("Language");
        }
        return new MetadataMatchDecision(candidate, Math.Min(score, 100), reasons);
    }

    private static int Priority(LibrariannMediaType mediaType, string providerKey) =>
        ProviderPriorities.TryGetValue(mediaType, out var priorities) && priorities.TryGetValue(providerKey, out var priority)
            ? priority
            : 0;

    private static string Normalize(string value)
    {
        var builder = new StringBuilder(value.Length);
        foreach (var character in value.Normalize(NormalizationForm.FormD))
        {
            if (char.IsLetterOrDigit(character)) builder.Append(char.ToLowerInvariant(character));
            else if (char.IsWhiteSpace(character) && builder.Length > 0 && builder[^1] != ' ') builder.Append(' ');
        }
        return builder.ToString().Trim();
    }

    private static string NormalizeLanguage(string value)
    {
        var normalized = Normalize(value);
        return normalized.Length > 2 ? normalized[..2] : normalized;
    }

    private static double TokenSimilarity(string first, string second)
    {
        var firstTokens = first.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToHashSet(StringComparer.Ordinal);
        var secondTokens = second.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToHashSet(StringComparer.Ordinal);
        if (firstTokens.Count == 0 || secondTokens.Count == 0) return 0;
        var intersection = firstTokens.Count(secondTokens.Contains);
        var union = firstTokens.Count + secondTokens.Count - intersection;
        return union == 0 ? 0 : (double) intersection / union;
    }

    private sealed record ProviderSearchResult(
        IReadOnlyCollection<NormalizedMetadataCandidate> Candidates,
        MetadataProviderFailure? Failure);
}
