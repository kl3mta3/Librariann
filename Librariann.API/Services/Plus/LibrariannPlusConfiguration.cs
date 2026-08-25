using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Librariann.Models.Entities.Enums;

namespace Librariann.API.Services.Plus;

/// <summary>
/// This class should hold readonly & static configuration for Librariann Plus. What can we used where etc.
/// </summary>
public static class LibrariannPlusConfiguration
{

    /// <remarks>When adjusting these, also adjust in LibrarySettingsModalComponent in the UI</remarks>
    public static readonly IReadOnlyDictionary<LibraryType, IReadOnlySet<MetadataProvider>> MetadataProvidersForLibraryTypes =
        new ReadOnlyDictionary<LibraryType, IReadOnlySet<MetadataProvider>>(
            new Dictionary<LibraryType, IReadOnlySet<MetadataProvider>>
            {
                [LibraryType.Manga] = new HashSet<MetadataProvider>
                {
                    MetadataProvider.Mangabaka
                },
                [LibraryType.Comic] = new HashSet<MetadataProvider>
                {
                    MetadataProvider.ComicBookRoundup,
                    MetadataProvider.Hardcover
                },
                [LibraryType.Book] = new HashSet<MetadataProvider>
                {
                    MetadataProvider.Hardcover,
                    MetadataProvider.Mangabaka
                },
                [LibraryType.Image] = new HashSet<MetadataProvider>
                {
                    MetadataProvider.Mangabaka,
                    MetadataProvider.ComicBookRoundup
                },
                [LibraryType.LightNovel] = new HashSet<MetadataProvider>
                {
                    MetadataProvider.Mangabaka,
                    MetadataProvider.Hardcover
                },
                [LibraryType.ComicVine] = new HashSet<MetadataProvider>
                {
                    MetadataProvider.ComicBookRoundup,
                    MetadataProvider.Hardcover
                }
            });

    /// <remarks>When adjusting these, also adjust in ManageScrobbleProvidersComponent in the UI</remarks>>
    public static readonly IReadOnlyDictionary<LibraryType, IReadOnlySet<ScrobbleProvider>> ScrobbleProvidersForLibraryTypes =
        new ReadOnlyDictionary<LibraryType, IReadOnlySet<ScrobbleProvider>>(
            new Dictionary<LibraryType, IReadOnlySet<ScrobbleProvider>>
            {
                [LibraryType.Book] = new HashSet<ScrobbleProvider>
                {
                    ScrobbleProvider.Hardcover
                },
                [LibraryType.LightNovel] = new HashSet<ScrobbleProvider>
                {
                    ScrobbleProvider.AniList,
                    ScrobbleProvider.Hardcover,
                    ScrobbleProvider.MangaBaka,
                    ScrobbleProvider.Mal
                },
                [LibraryType.Comic] = new HashSet<ScrobbleProvider>
                {
                    ScrobbleProvider.Hardcover
                },
                [LibraryType.ComicVine] = new HashSet<ScrobbleProvider>
                {
                    ScrobbleProvider.Hardcover
                },
                [LibraryType.Manga] = new HashSet<ScrobbleProvider>
                {
                    ScrobbleProvider.AniList,
                    ScrobbleProvider.MangaBaka,
                    ScrobbleProvider.Mal
                }
            });

    /// <summary>
    /// All currently in use scrobble providers. Generated from <see cref="ScrobbleProvidersForLibraryTypes"/>
    /// </summary>
    public static readonly IReadOnlyList<ScrobbleProvider> AllInUseScrobbleProviders = ScrobbleProvidersForLibraryTypes
        .Values.SelectMany(s => s).Distinct().ToList().AsReadOnly();

    /// <summary>
    /// Is the given <see cref="MetadataProvider"/> valid (supported) for the given <see cref="LibraryType"/>.
    /// Used to validate both a Library's default provider and a Series-level provider override.
    /// </summary>
    /// <remarks>
    /// A LibraryType with no entry in <see cref="MetadataProvidersForLibraryTypes"/> (e.g. Audiobook, which has
    /// no Librariann+ metadata target at all) has no metadata-provider concept to validate against - treat that
    /// as "nothing to reject" rather than "every provider is invalid", or every library of that type would fail
    /// to save regardless of which provider value the client happens to send.
    /// </remarks>
    public static bool IsValidMetadataProviderForLibraryType(LibraryType type, MetadataProvider provider)
    {
        if (!MetadataProvidersForLibraryTypes.TryGetValue(type, out var validProviders)) return true;
        return validProviders.Contains(provider);
    }

}
