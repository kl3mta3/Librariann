using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using Librariann.API.Database;
using Librariann.API.Repositories;
using Librariann.API.Services;
using Librariann.Common;
using Librariann.Common.EnvironmentInfo;
using Librariann.Models;
using Librariann.Models.Constants;
using Librariann.Models.DTOs.LibrariannPlus.Scrobble;
using Librariann.Models.DTOs.Settings;
using Librariann.Models.DTOs.Account;
using Librariann.Models.Entities;
using Librariann.Models.Entities.Acquisition;
using Librariann.Models.Entities.Enums;
using Librariann.Models.Entities.MetadataMatching;
using Librariann.Models.Entities.User;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Librariann.Database;

public static class Seed
{

    public static async Task SeedRoles(RoleManager<AppRole> roleManager)
    {
        var roles = typeof(PolicyConstants)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(f => f.FieldType == typeof(string))
            .ToDictionary(f => f.Name,
                f => (string) f.GetValue(null)!).Values
            .Select(policyName => new AppRole() {Name = policyName})
            .ToList();

        foreach (var role in roles)
        {
            var exists = await roleManager.RoleExistsAsync(role.Name!);
            if (!exists)
            {
                await roleManager.CreateAsync(role);
            }
        }
    }

    public static async Task SeedFonts(IDataContext context)
    {
        await context.Database.EnsureCreatedAsync();

        foreach (var font in Defaults.DefaultFonts)
        {
            var existing = await context.EpubFont.FirstOrDefaultAsync(f => f.Name.Equals(font.Name));
            if (existing == null)
            {
                await context.EpubFont.AddAsync(font);
            }
        }

        await context.SaveChangesAsync();
    }

    public static async Task SeedDefaultStreams(IUnitOfWork unitOfWork)
    {
        var allUsers = await unitOfWork.UserRepository.GetAllUsersAsync(AppUserIncludes.DashboardStreams);
        foreach (var user in allUsers)
        {
            if (user.DashboardStreams.Count != 0) continue;
            user.DashboardStreams ??= [];
            foreach (var defaultStream in Defaults.DefaultStreams)
            {
                var newStream = new AppUserDashboardStream
                {
                    Name = defaultStream.Name,
                    IsProvided = defaultStream.IsProvided,
                    Order = defaultStream.Order,
                    StreamType = defaultStream.StreamType,
                    Visible = defaultStream.Visible,
                };

                user.DashboardStreams.Add(newStream);
            }
            unitOfWork.UserRepository.Update(user);
            await unitOfWork.CommitAsync();
        }
    }

    public static async Task SeedDefaultSideNavStreams(IUnitOfWork unitOfWork)
    {
        var allUsers = await unitOfWork.UserRepository.GetAllUsersAsync(AppUserIncludes.SideNavStreams);
        foreach (var user in allUsers)
        {
            user.SideNavStreams ??= [];
            foreach (var defaultStream in Defaults.DefaultSideNavStreams)
            {
                if (user.SideNavStreams.Any(s => s.Name == defaultStream.Name && s.StreamType == defaultStream.StreamType)) continue;
                var newStream = new AppUserSideNavStream()
                {
                    Name = defaultStream.Name,
                    IsProvided = defaultStream.IsProvided,
                    Order = defaultStream.Order,
                    StreamType = defaultStream.StreamType,
                    Visible = defaultStream.Visible,
                };

                user.SideNavStreams.Add(newStream);
            }
            unitOfWork.UserRepository.Update(user);
            await unitOfWork.CommitAsync();
        }
    }

    public static async Task SeedDefaultHighlightSlots(IUnitOfWork unitOfWork)
    {
        var allUsers = await unitOfWork.UserRepository.GetAllUsersAsync(AppUserIncludes.UserPreferences);
        foreach (var user in allUsers)
        {
            if (user.UserPreferences.BookReaderHighlightSlots.Any()) break;

            user.UserPreferences.BookReaderHighlightSlots = Defaults.DefaultHighlightSlots.ToList();
            unitOfWork.UserRepository.Update(user);
        }
        await unitOfWork.CommitAsync();
    }

    public static async Task SeedSettings(IDataContext context, IDirectoryService directoryService)
    {
        await context.Database.EnsureCreatedAsync();
        Defaults.DefaultSettings = [
            ..new List<ServerSetting>()
            {
                new() {Key = ServerSettingKey.CacheDirectory, Value = directoryService.CacheDirectory},
                new() {Key = ServerSettingKey.TaskScan, Value = "daily"},
                new() {Key = ServerSettingKey.TaskBackup, Value = "daily"},
                new() {Key = ServerSettingKey.TaskCleanup, Value = "daily"},
                new() {Key = ServerSettingKey.TaskCblSync, Value = "0 4 * * *"}, // 4am daily
                new() {Key = ServerSettingKey.LoggingLevel, Value = "Debug"},
                new()
                {
                    Key = ServerSettingKey.BackupDirectory, Value = Path.GetFullPath(directoryService.BackupDirectory)
                },
                new()
                {
                    Key = ServerSettingKey.Port, Value = Configuration.DefaultHttpPort + string.Empty
                }, // Not used from DB, but DB is sync with appSettings.json
                new() {
                    Key = ServerSettingKey.IpAddresses, Value = Configuration.DefaultIpAddresses
                }, // Not used from DB, but DB is sync with appSettings.json
                new() {Key = ServerSettingKey.EnableOpds, Value = "true"},
                new() {Key = ServerSettingKey.BaseUrl, Value = "/"},
                new() {Key = ServerSettingKey.InstallId, Value = HashUtil.AnonymousToken()},
                new() {Key = ServerSettingKey.InstallVersion, Value = BuildInfo.Version.ToString()},
                new() {Key = ServerSettingKey.BookmarkDirectory, Value = directoryService.BookmarkDirectory},
                new() {Key = ServerSettingKey.TotalBackups, Value = "30"},
                new() {Key = ServerSettingKey.TotalLogs, Value = "30"},
                new() {Key = ServerSettingKey.EnableFolderWatching, Value = "false"},
                new() {Key = ServerSettingKey.WriteMetadataToFiles, Value = "false"},
                new() {Key = ServerSettingKey.HostName, Value = string.Empty},
                new() {Key = ServerSettingKey.EncodeMediaAs, Value = nameof(EncodeFormat.PNG)},
                new() {Key = ServerSettingKey.LicenseKey, Value = string.Empty},
                new() {Key = ServerSettingKey.OnDeckProgressDays, Value = "30"},
                new() {Key = ServerSettingKey.OnDeckUpdateDays, Value = "7"},
                new() {Key = ServerSettingKey.CoverImageSize, Value = nameof(CoverImageSize.Default)},
                new() {Key = ServerSettingKey.PdfRenderResolution, Value = nameof(PdfRenderResolution.Default)},
                new() {
                    Key = ServerSettingKey.CacheSize, Value = Configuration.DefaultCacheMemory + string.Empty
                }, // Not used from DB, but DB is sync with appSettings.json
                new() { Key = ServerSettingKey.OidcConfiguration, Value = JsonSerializer.Serialize(new OidcConfigDto())},

                new() {Key = ServerSettingKey.EmailHost, Value = string.Empty},
                new() {Key = ServerSettingKey.EmailPort, Value = string.Empty},
                new() {Key = ServerSettingKey.EmailAuthPassword, Value = string.Empty},
                new() {Key = ServerSettingKey.EmailAuthUserName, Value = string.Empty},
                new() {Key = ServerSettingKey.EmailSenderAddress, Value = string.Empty},
                new() {Key = ServerSettingKey.EmailSenderDisplayName, Value = string.Empty},
                new() {Key = ServerSettingKey.EmailEnableSsl, Value = "true"},
                new() {Key = ServerSettingKey.EmailSizeLimit, Value = 26_214_400 + string.Empty},
                new() {Key = ServerSettingKey.EmailCustomizedTemplates, Value = "false"},
                new() {Key = ServerSettingKey.FirstInstallVersion, Value = BuildInfo.Version.ToString()},
                new() {Key = ServerSettingKey.FirstInstallDate, Value = DateTime.UtcNow.ToString(CultureInfo.InvariantCulture)},
                new() {Key = ServerSettingKey.FfmpegPath, Value = "ffmpeg"},
                new() {Key = ServerSettingKey.MetadataProviderContactEmail, Value = string.Empty},
                new() {Key = ServerSettingKey.KokoroEndpointUrl, Value = string.Empty},
                new() {Key = ServerSettingKey.KokoroExecutablePath, Value = string.Empty},
                new() {Key = ServerSettingKey.KokoroUseGpu, Value = "false"},
                new() {Key = ServerSettingKey.KokoroSyncFfmpegPath, Value = "true"},
                new() {Key = ServerSettingKey.ShowRequestInviteLink, Value = "false"},
                new() {Key = ServerSettingKey.AutoAcceptInviteRequests, Value = "false"},
                new() {Key = ServerSettingKey.DefaultInvitePermissions, Value = JsonSerializer.Serialize(new DefaultInvitePermissionsDto())},
            }.ToArray()
        ];

        foreach (var defaultSetting in Defaults.DefaultSettings)
        {
            var existing = await context.ServerSetting.FirstOrDefaultAsync(s => s.Key == defaultSetting.Key);
            if (existing == null)
            {
                await context.ServerSetting.AddAsync(defaultSetting);
            }
        }

        await context.SaveChangesAsync();

        // Port, IpAddresses and LoggingLevel are managed in appSettings.json. Update the DB values to match
        (await context.ServerSetting.FirstAsync(s => s.Key == ServerSettingKey.Port)).Value =
            Configuration.Port + string.Empty;
        (await context.ServerSetting.FirstAsync(s => s.Key == ServerSettingKey.IpAddresses)).Value =
            Configuration.IpAddresses;
        (await context.ServerSetting.FirstAsync(s => s.Key == ServerSettingKey.CacheDirectory)).Value =
            directoryService.CacheDirectory + string.Empty;
        (await context.ServerSetting.FirstAsync(s => s.Key == ServerSettingKey.BackupDirectory)).Value =
            directoryService.BackupDirectory + string.Empty;
        (await context.ServerSetting.FirstAsync(s => s.Key == ServerSettingKey.CacheSize)).Value =
            Configuration.CacheSize + string.Empty;

        await SetOidcSettingsFromDisk(context);


        await context.SaveChangesAsync();
    }

    public static async Task SetOidcSettingsFromDisk(IDataContext context)
    {
        var oidcSettingEntry = await context.ServerSetting
            .FirstOrDefaultAsync(setting => setting.Key == ServerSettingKey.OidcConfiguration);

        var storedOidcSettings = JsonSerializer.Deserialize<OidcConfigDto>(oidcSettingEntry!.Value)!;

        var diskOidcSettings = Configuration.OidcSettings;

        storedOidcSettings.Authority = diskOidcSettings.Authority;
        storedOidcSettings.ClientId = diskOidcSettings.ClientId;
        storedOidcSettings.Secret = diskOidcSettings.Secret;
        storedOidcSettings.CustomScopes = diskOidcSettings.CustomScopes;

        oidcSettingEntry.Value = JsonSerializer.Serialize(storedOidcSettings);
    }

    public static async Task SeedMetadataSettings(IDataContext context)
    {
        await context.Database.EnsureCreatedAsync();

        var existing = await context.MetadataSettings.FirstOrDefaultAsync();
        if (existing == null)
        {
            existing = new MetadataSettings()
            {
                Enabled = true,
                EnablePeople = true,
                EnableRelationships = true,
                EnableSummary = true,
                EnablePublicationStatus = true,
                EnableStartDate = true,
                EnableTags = false,
                EnableGenres = true,
                EnableLocalizedName = false,
                EnableName = false,
                FirstLastPeopleNaming = true,
                EnableCoverImage = true,
                EnableChapterTitle = false,
                EnableChapterSummary = true,
                EnableChapterPublisher = true,
                EnableChapterCoverImage = false,
                EnableChapterReleaseDate = true,
                PersonRoles = [PersonRole.Writer, PersonRole.CoverArtist, PersonRole.Character],
                GlobalNameLanguages = "en",
                GlobalLocalizedNameLanguages = "ja-Latn",
                LibraryLanguageTitleOverrides = []
            };
            await context.MetadataSettings.AddAsync(existing);
        }


        await context.SaveChangesAsync();

    }

    /// <summary>
    /// Ensures a free, keyless Open Library metadata provider is configured and enabled out of the box, so the
    /// Metadata Discovery page (and any future auto-fetch-on-scan feature) isn't silently useless on a fresh
    /// install just because nobody has visited Settings -> Indexers & Downloads yet. Runs once - tracked by
    /// <see cref="ServerSettingKey.DefaultMetadataProviderSeeded"/> - so an admin who deliberately deletes it
    /// doesn't see it reappear on the next restart.
    /// </summary>
    public static async Task SeedDefaultMetadataProviders(IDataContext context)
    {
        await context.Database.EnsureCreatedAsync();

        var seededMarker = await context.ServerSetting
            .FirstOrDefaultAsync(s => s.Key == ServerSettingKey.DefaultMetadataProviderSeeded);
        if (seededMarker is {Value: "true"}) return;

        var hasMetadataProvider = await context.IntegrationProviderConfigurations
            .AnyAsync(p => p.Category == IntegrationProviderCategory.Metadata);
        if (!hasMetadataProvider)
        {
            await context.IntegrationProviderConfigurations.AddAsync(new IntegrationProviderConfiguration
            {
                Name = "Open Library",
                Category = IntegrationProviderCategory.Metadata,
                ProviderType = "open-library",
                BaseUrl = "https://openlibrary.org",
                IsEnabled = true,
            });
        }

        if (seededMarker != null)
        {
            seededMarker.Value = "true";
        }
        else
        {
            await context.ServerSetting.AddAsync(new ServerSetting
            {
                Key = ServerSettingKey.DefaultMetadataProviderSeeded,
                Value = "true",
            });
        }

        await context.SaveChangesAsync();
    }

    public static async Task SeedScrobbleProviders(IDataContext context)
    {
        await context.Database.EnsureCreatedAsync();

        var allUsers = await context.Users.ToListAsync();

        foreach (var user in allUsers)
        {
            var missingProviders = Enum.GetValues<ScrobbleProvider>()
                .Where(p => p != ScrobbleProvider.Librariann && p != ScrobbleProvider.Cbr)
                .Where(p => !user.ScrobbleProviders.ContainsKey(p))
                .ToList();

            foreach (var provider in missingProviders)
            {
                user.ScrobbleProviders[provider] = new AppUserScrobbleProvider
                {
                    Provider = provider,
                    Settings = new ScrobbleProviderSettingsDto()
                    {

                        ProgressScrobbling = true,
                        RatingScrobbling = true,
                        WantToReadSync = true,
                        AllLibraries = true
                    }
                };
            }

            if (missingProviders.Count > 0)
            {
                context.Users.Update(user);
            }
        }

        if (context.ChangeTracker.HasChanges())
        {
            await context.SaveChangesAsync();
        }
    }
}
