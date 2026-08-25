using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Librariann.API.Database;
using Librariann.API.Services;
using Librariann.API.Services.Acquisition;
using Librariann.Models.DTOs.Acquisition;
using Librariann.Models.Entities.Acquisition;

namespace Librariann.Services.Acquisition;

public sealed class InteractiveSearchService(
    IUnitOfWork unitOfWork,
    ICredentialProtectionService credentialProtection,
    IIntegrationHttpClientFactory httpClientFactory,
    IReleaseEvaluationService evaluator,
    IReleaseTokenStore releaseTokenStore) : IInteractiveSearchService
{
    public async Task<InteractiveSearchResponse> SearchAsync(int userId, InteractiveSearchRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = await SearchCoreAsync(request, cancellationToken);
        return response with
        {
            Results = response.Results.Select(result => result.IsApproved && result.Release.DownloadUri is not null
                ? result with {GrabToken = releaseTokenStore.Issue(userId, result.Release)}
                : result).ToArray(),
        };
    }

    public Task<InteractiveSearchResponse> SearchForAutomationAsync(InteractiveSearchRequest request,
        CancellationToken cancellationToken = default) => SearchCoreAsync(request, cancellationToken);

    private async Task<InteractiveSearchResponse> SearchCoreAsync(InteractiveSearchRequest request,
        CancellationToken cancellationToken)
    {
        var qualityProfile = await unitOfWork.QualityProfileRepository.GetAsync(request.QualityProfileId, cancellationToken)
                             ?? throw new Librariann.Common.LibrariannException("quality-profile-does-not-exist");
        var evaluation = request.Evaluation with
        {
            AllowedLanguages = string.IsNullOrWhiteSpace(qualityProfile.Language) ? [] : [qualityProfile.Language],
            FormatScores = qualityProfile.FormatScores,
            MinimumSizeBytes = qualityProfile.MinimumSizeBytes,
            MaximumSizeBytes = qualityProfile.MaximumSizeBytes,
            PreferRetail = qualityProfile.PreferRetail,
            UpgradeAllowed = qualityProfile.UpgradeAllowed,
            CutoffScore = qualityProfile.FormatScores.GetValueOrDefault(qualityProfile.CutoffFormat),
        };
        request = request with {Evaluation = evaluation};
        var configurations = (await unitOfWork.IntegrationProviderRepository.GetAllAsync(cancellationToken))
            .Where(provider => provider.IsEnabled && provider.Category == IntegrationProviderCategory.Indexer &&
                               provider.IndexerProtocol.HasValue)
            .ToArray();

        var batches = await Task.WhenAll(configurations.Select(configuration =>
            SearchProviderAsync(configuration, request, cancellationToken)));

        var results = batches.SelectMany(batch => batch.Results)
            .OrderByDescending(result => result.IsApproved)
            .ThenByDescending(result => result.Score)
            .ThenByDescending(result => result.Release.Seeders ?? -1)
            .ToArray();
        var failures = batches.Where(batch => batch.Failure is not null).Select(batch => batch.Failure!).ToArray();
        return new InteractiveSearchResponse(results, failures);
    }

    private async Task<SearchBatch> SearchProviderAsync(IntegrationProviderConfiguration configuration,
        InteractiveSearchRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var apiKey = ReadSecret(configuration, configuration.ProtectedApiKey, "api-key");
            var username = ReadSecret(configuration, configuration.ProtectedUsername, "username");
            var password = ReadSecret(configuration, configuration.ProtectedPassword, "password");
            var client = httpClientFactory.Create(configuration);
            if (!string.IsNullOrEmpty(username))
            {
                var basic = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{password}"));
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", basic);
            }

            using var provider = new NewznabIndexerProvider($"integration-{configuration.Id}",
                configuration.IndexerProtocol!.Value, client, apiKey);
            var releases = await provider.SearchAsync(request.Search, cancellationToken);
            return new SearchBatch(releases.Select(release => evaluator.Evaluate(release, request.Evaluation)).ToArray(), null);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            return new SearchBatch([], new ProviderSearchFailure($"integration-{configuration.Id}",
                configuration.Name, "The provider search failed. Test the provider connection and credentials."));
        }
    }

    private string ReadSecret(IntegrationProviderConfiguration configuration, string protectedValue, string field)
    {
        if (string.IsNullOrEmpty(protectedValue)) return string.Empty;
        return credentialProtection.Unprotect(protectedValue, IntegrationCredentialScope.For(configuration, field));
    }

    private sealed record SearchBatch(IReadOnlyCollection<ReleaseDecision> Results, ProviderSearchFailure? Failure);
}
