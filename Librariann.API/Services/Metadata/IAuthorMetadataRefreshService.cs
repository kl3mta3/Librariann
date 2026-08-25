using System.Threading.Tasks;

namespace Librariann.API.Services.Metadata;

public interface IAuthorMetadataRefreshService
{
    /// <summary>
    /// Refreshes matched writers and safely auto-matches writers having one unambiguous exact-name result.
    /// Ambiguous matches are intentionally left for the administrator to resolve interactively.
    /// </summary>
    Task RefreshAllAsync();
}
