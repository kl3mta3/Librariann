using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Librariann.API.Database;
using Librariann.API.Repositories;
using Librariann.API.Services;
using Librariann.API.Services.SignalR;
using Librariann.Common;
using Librariann.Common.Helpers;
using Librariann.Models.DTOs.Dashboard;
using Librariann.Models.DTOs.SideNav;
using Librariann.Models.DTOs.SignalR;
using Librariann.Models.Entities;
using Librariann.Models.Entities.Enums;
using Librariann.Models.Entities.User;
using Librariann.Models.Helpers;
using Microsoft.Extensions.Logging;

namespace Librariann.Services;

public class StreamService(
    IUnitOfWork unitOfWork,
    IEventHub eventHub,
    ILocalizationService localizationService,
    ILogger<StreamService> logger,
    ICredentialProtectionService credentialProtectionService,
    IExternalSourceLaunchTokenStore externalSourceLaunchTokenStore)
    : IStreamService
{
    public async Task ProtectStoredCredentialsAsync(CancellationToken ct = default)
    {
        var protectedCount = 0;
        var sources = await unitOfWork.AppUserExternalSourceRepository.GetAll(ct);
        foreach (var source in sources)
        {
            if (string.IsNullOrEmpty(source.ApiKey) || credentialProtectionService.IsProtected(source.ApiKey))
                continue;

            source.ApiKey = credentialProtectionService.Protect(source.ApiKey,
                ServerSettingCredentialScopes.ExternalSourceApiKey(source.AppUserId));
            unitOfWork.AppUserExternalSourceRepository.Update(source);
            protectedCount++;
        }

        if (protectedCount == 0) return;
        await unitOfWork.CommitAsync(ct);
        logger.LogInformation("Protected {CredentialCount} external-source API key(s) at rest", protectedCount);
    }

    public async Task<IEnumerable<DashboardStreamDto>> GetDashboardStreams(int userId, bool visibleOnly = true,
        CancellationToken ct = default)
    {
        return await unitOfWork.UserRepository.GetDashboardStreams(userId, visibleOnly, ct);
    }

    public async Task<IEnumerable<SideNavStreamDto>> GetSidenavStreams(int userId, bool visibleOnly = true, CancellationToken ct = default)
    {
        var streams = await unitOfWork.UserRepository.GetSideNavStreams(userId, visibleOnly, ct);
        foreach (var stream in streams.Where(stream => stream.ExternalSource != null))
        {
            MaskExternalSource(stream.ExternalSource!);
        }

        return streams;
    }

    public async Task<IEnumerable<ExternalSourceDto>> GetExternalSources(int userId, CancellationToken ct = default)
    {
        var sources = await unitOfWork.AppUserExternalSourceRepository.GetExternalSources(userId, ct);
        foreach (var source in sources.Where(source => !string.IsNullOrEmpty(source.ApiKey)))
        {
            source.ApiKey = ServerSettingCredentialScopes.MaskedValue;
        }
        return sources;
    }

    public async Task<DashboardStreamDto> CreateDashboardStreamFromSmartFilter(int userId, int smartFilterId,
        CancellationToken ct = default)
    {
        var user = await unitOfWork.UserRepository.GetUserByIdAsync(userId, AppUserIncludes.DashboardStreams, ct);
        if (user == null) throw new LibrariannException(await localizationService.TranslateAsync(userId, "no-user"));

        var smartFilter = await unitOfWork.AppUserSmartFilterRepository.GetById(smartFilterId, ct);
        if (smartFilter == null) throw new LibrariannException(await localizationService.TranslateAsync(userId, "smart-filter-doesnt-exist"));

        var stream = user.DashboardStreams.FirstOrDefault(d => d.SmartFilter?.Id == smartFilterId);
        if (stream != null) throw new LibrariannException(await localizationService.TranslateAsync(userId, "smart-filter-already-in-use"));

        var maxOrder = user!.DashboardStreams.Max(d => d.Order);
        var createdStream = new AppUserDashboardStream()
        {
            Name = smartFilter.Name,
            IsProvided = false,
            StreamType = DashboardStreamType.SmartFilter,
            Visible = true,
            Order = maxOrder + 1,
            SmartFilter = smartFilter
        };

        user.DashboardStreams.Add(createdStream);
        unitOfWork.UserRepository.Update(user);
        await unitOfWork.CommitAsync(ct);

        var ret = new DashboardStreamDto()
        {
            Id = createdStream.Id,
            Name = createdStream.Name,
            IsProvided = createdStream.IsProvided,
            Visible = createdStream.Visible,
            Order = createdStream.Order,
            SmartFilterEncoded = smartFilter.Filter,
            StreamType = createdStream.StreamType
        };

        await eventHub.SendMessageToAsync(MessageFactory.DashboardUpdate, MessageFactory.DashboardUpdateEvent(user.Id),
            userId, ct);

        return ret;
    }

    public async Task UpdateDashboardStream(int userId, DashboardStreamDto dto, CancellationToken ct = default)
    {
        var stream = await unitOfWork.UserRepository.GetDashboardStream(dto.Id, ct);
        if (stream == null) throw new LibrariannException(await localizationService.TranslateAsync(userId, "dashboard-stream-doesnt-exist"));

        if (stream.AppUserId != userId)
        {
            throw new LibrariannNotFoundException();
        }

        stream.Visible = dto.Visible;

        unitOfWork.UserRepository.Update(stream);
        await unitOfWork.CommitAsync(ct);
        await eventHub.SendMessageToAsync(MessageFactory.DashboardUpdate, MessageFactory.DashboardUpdateEvent(userId),
            userId, ct);
    }

    public async Task UpdateDashboardStreamPosition(int userId, UpdateStreamPositionDto dto,
        CancellationToken ct = default)
    {
        var user = await unitOfWork.UserRepository.GetUserByIdAsync(userId,
            AppUserIncludes.DashboardStreams, ct);
        var stream = user?.DashboardStreams.FirstOrDefault(d => d.Id == dto.Id);
        if (stream == null)
        {
            throw new LibrariannException(await localizationService.TranslateAsync(userId, "dashboard-stream-doesnt-exist"));
        }

        if (stream.Order == dto.ToPosition) return;

        var list = user!.DashboardStreams.OrderBy(s => s.Order).ToList();
        OrderableHelper.ReorderItems(list, stream.Id, dto.ToPosition);
        user.DashboardStreams = list;

        unitOfWork.UserRepository.Update(user);
        await unitOfWork.CommitAsync(ct);
        if (!stream.Visible) return;
        await eventHub.SendMessageToAsync(MessageFactory.DashboardUpdate, MessageFactory.DashboardUpdateEvent(user.Id),
            user.Id, ct);
    }

    public async Task UpdateSideNavStreamBulk(int userId, BulkUpdateSideNavStreamVisibilityDto dto,
        CancellationToken ct = default)
    {
        var streams = await unitOfWork.UserRepository.GetDashboardStreamsByIds(dto.Ids, ct);
        foreach (var stream in streams)
        {
            stream.Visible = dto.Visibility;
            unitOfWork.UserRepository.Update(stream);
        }

        await unitOfWork.CommitAsync(ct);
        await eventHub.SendMessageToAsync(MessageFactory.SideNavUpdate, MessageFactory.SideNavUpdateEvent(userId),
            userId, ct);
    }

    public async Task<SideNavStreamDto> CreateSideNavStreamFromSmartFilter(int userId, int smartFilterId,
        CancellationToken ct = default)
    {
        var user = await unitOfWork.UserRepository.GetUserByIdAsync(userId, AppUserIncludes.SideNavStreams, ct);
        if (user == null) throw new LibrariannException(await localizationService.TranslateAsync(userId, "no-user"));

        var smartFilter = await unitOfWork.AppUserSmartFilterRepository.GetById(smartFilterId, ct);
        if (smartFilter == null) throw new LibrariannException(await localizationService.TranslateAsync(userId, "smart-filter-doesnt-exist"));

        var stream = user.SideNavStreams.FirstOrDefault(d => d.SmartFilter?.Id == smartFilterId);
        if (stream != null) throw new LibrariannException(await localizationService.TranslateAsync(userId, "smart-filter-already-in-use"));

        var maxOrder = user!.SideNavStreams.Max(d => d.Order);
        var createdStream = new AppUserSideNavStream()
        {
            Name = smartFilter.Name,
            IsProvided = false,
            StreamType = SideNavStreamType.SmartFilter,
            Visible = true,
            Order = maxOrder + 1,
            SmartFilter = smartFilter
        };

        user.SideNavStreams.Add(createdStream);
        unitOfWork.UserRepository.Update(user);
        await unitOfWork.CommitAsync(ct);

        var ret = new SideNavStreamDto()
        {
            Id = createdStream.Id,
            Name = createdStream.Name,
            IsProvided = createdStream.IsProvided,
            Visible = createdStream.Visible,
            Order = createdStream.Order,
            SmartFilterEncoded = smartFilter.Filter,
            StreamType = createdStream.StreamType
        };
        MaskExternalSource(ret.ExternalSource!);


        await eventHub.SendMessageToAsync(MessageFactory.SideNavUpdate, MessageFactory.SideNavUpdateEvent(userId),
            userId, ct);
        return ret;
    }

    public async Task<SideNavStreamDto> CreateSideNavStreamFromExternalSource(int userId, int externalSourceId,
        CancellationToken ct = default)
    {
        var user = await unitOfWork.UserRepository.GetUserByIdAsync(userId, AppUserIncludes.SideNavStreams, ct);
        if (user == null) throw new LibrariannException(await localizationService.TranslateAsync(userId, "no-user"));

        var externalSource = await unitOfWork.AppUserExternalSourceRepository.GetById(externalSourceId, ct);
        if (externalSource == null) throw new LibrariannException(await localizationService.TranslateAsync(userId, "external-source-doesnt-exist"));

        var stream = user?.SideNavStreams.FirstOrDefault(d => d.ExternalSourceId == externalSourceId);
        if (stream != null) throw new LibrariannException(await localizationService.TranslateAsync(userId, "external-source-already-in-use"));

        var maxOrder = user!.SideNavStreams.Max(d => d.Order);
        var createdStream = new AppUserSideNavStream()
        {
            Name = externalSource.Name,
            IsProvided = false,
            StreamType = SideNavStreamType.ExternalSource,
            Visible = true,
            Order = maxOrder + 1,
            ExternalSourceId = externalSource.Id
        };

        user.SideNavStreams.Add(createdStream);
        unitOfWork.UserRepository.Update(user);
        await unitOfWork.CommitAsync(ct);

        var ret = new SideNavStreamDto()
        {
            Name = createdStream.Name,
            IsProvided = createdStream.IsProvided,
            Visible = createdStream.Visible,
            Order = createdStream.Order,
            StreamType = createdStream.StreamType,
            ExternalSource = new ExternalSourceDto()
            {
                Host = externalSource.Host,
                Id = externalSource.Id,
                Name = externalSource.Name,
                ApiKey = credentialProtectionService.IsProtected(externalSource.ApiKey)
                    ? credentialProtectionService.Unprotect(externalSource.ApiKey,
                        ServerSettingCredentialScopes.ExternalSourceApiKey(userId))
                    : externalSource.ApiKey
            }
        };


        await eventHub.SendMessageToAsync(MessageFactory.SideNavUpdate, MessageFactory.SideNavUpdateEvent(userId),
            userId, ct);
        return ret;
    }

    public async Task UpdateSideNavStream(int userId, SideNavStreamDto dto, CancellationToken ct = default)
    {
        var stream = await unitOfWork.UserRepository.GetSideNavStream(dto.Id, ct);
        if (stream == null)
            throw new LibrariannException(await localizationService.TranslateAsync(userId, "sidenav-stream-doesnt-exist"));

        stream.Visible = dto.Visible;

        unitOfWork.UserRepository.Update(stream);
        await unitOfWork.CommitAsync(ct);
        await eventHub.SendMessageToAsync(MessageFactory.SideNavUpdate, MessageFactory.SideNavUpdateEvent(userId),
            userId, ct);
    }

    public async Task UpdateSideNavStreamPosition(int userId, UpdateStreamPositionDto dto, CancellationToken ct = default)
    {
        var user = await unitOfWork.UserRepository.GetUserByIdAsync(userId,
            AppUserIncludes.SideNavStreams, ct);
        var stream = user?.SideNavStreams.FirstOrDefault(d => d.Id == dto.Id);
        if (stream == null) throw new LibrariannException(await localizationService.TranslateAsync(userId, "sidenav-stream-doesnt-exist"));

        if (stream.Order == dto.ToPosition) return;

        var list = user!.SideNavStreams.OrderBy(s => s.Order).ToList();

        var wantedPosition = dto.ToPosition;
        if (!dto.PositionIncludesInvisible)
        {
            var visibleItems = list.Where(i => i.Visible).ToList();
            if (dto.ToPosition < 0 || dto.ToPosition >= visibleItems.Count) return;

            var itemAtWantedPosition = visibleItems[dto.ToPosition];
            wantedPosition = list.IndexOf(itemAtWantedPosition);
        }

        OrderableHelper.ReorderItems(list, stream.Id, wantedPosition);
        user.SideNavStreams = list;

        unitOfWork.UserRepository.Update(user);
        await unitOfWork.CommitAsync(ct);
        if (!stream.Visible) return;
        await eventHub.SendMessageToAsync(MessageFactory.SideNavUpdate, MessageFactory.SideNavUpdateEvent(userId),
            userId, ct);
    }

    public async Task<ExternalSourceDto> CreateExternalSource(int userId, ExternalSourceDto dto,
        CancellationToken ct = default)
    {
        var user = await unitOfWork.UserRepository.GetUserByIdAsync(userId,
            AppUserIncludes.ExternalSources, ct);
        if (user == null) throw new LibrariannException("not-authenticated");

        if (user.ExternalSources.Any(s => s.Host == dto.Host))
        {
            throw new LibrariannException("external-source-already-exists");
        }

        if (string.IsNullOrEmpty(dto.Name)) throw new LibrariannException("external-source-required");
        if (!UrlHelper.StartsWithHttpOrHttps(dto.Host)) throw new LibrariannException("external-source-host-format");


        var newSource = new AppUserExternalSource()
        {
            Name = dto.Name,
            Host = UrlHelper.EnsureEndsWithSlash(UrlHelper.EnsureStartsWithHttpOrHttps(dto.Host)),
            ApiKey = string.IsNullOrEmpty(dto.ApiKey)
                ? string.Empty
                : credentialProtectionService.Protect(dto.ApiKey,
                    ServerSettingCredentialScopes.ExternalSourceApiKey(userId))
        };
        user.ExternalSources.Add(newSource);

        unitOfWork.UserRepository.Update(user);
        await unitOfWork.CommitAsync(ct);

        dto.Id = newSource.Id;
        if (!string.IsNullOrEmpty(dto.ApiKey)) dto.ApiKey = ServerSettingCredentialScopes.MaskedValue;

        return dto;
    }

    public async Task<ExternalSourceDto> UpdateExternalSource(int userId, ExternalSourceDto dto,
        CancellationToken ct = default)
    {
        var source = await unitOfWork.AppUserExternalSourceRepository.GetById(dto.Id, ct);
        if (source == null) throw new LibrariannException("external-source-doesnt-exist");
        if (source.AppUserId != userId) throw new LibrariannException("external-source-doesnt-exist");

        if (string.IsNullOrEmpty(dto.Host) || string.IsNullOrEmpty(dto.Name)) throw new LibrariannException("external-source-required");

        source.Host = UrlHelper.EnsureEndsWithSlash(
            UrlHelper.EnsureStartsWithHttpOrHttps(dto.Host));
        var currentApiKey = credentialProtectionService.IsProtected(source.ApiKey)
            ? credentialProtectionService.Unprotect(source.ApiKey,
                ServerSettingCredentialScopes.ExternalSourceApiKey(userId))
            : source.ApiKey;
        if (dto.ApiKey == ServerSettingCredentialScopes.MaskedValue)
        {
            dto.ApiKey = currentApiKey;
        }
        source.ApiKey = credentialProtectionService.IsProtected(source.ApiKey) && dto.ApiKey == currentApiKey
            ? source.ApiKey
            : string.IsNullOrEmpty(dto.ApiKey)
                ? string.Empty
                : credentialProtectionService.Protect(dto.ApiKey,
                    ServerSettingCredentialScopes.ExternalSourceApiKey(userId));
        source.Name = dto.Name;

        unitOfWork.AppUserExternalSourceRepository.Update(source);
        await unitOfWork.CommitAsync(ct);

        dto.Host = source.Host;
        if (!string.IsNullOrEmpty(dto.ApiKey)) dto.ApiKey = ServerSettingCredentialScopes.MaskedValue;
        return dto;
    }

    public async Task<string> CreateExternalSourceLaunchAsync(int userId, int externalSourceId,
        CancellationToken ct = default)
    {
        var source = await unitOfWork.AppUserExternalSourceRepository.GetById(externalSourceId, ct);
        if (source == null || source.AppUserId != userId) throw new LibrariannException("external-source-doesnt-exist");

        var apiKey = credentialProtectionService.IsProtected(source.ApiKey)
            ? credentialProtectionService.Unprotect(source.ApiKey,
                ServerSettingCredentialScopes.ExternalSourceApiKey(userId))
            : source.ApiKey;
        if (string.IsNullOrEmpty(apiKey))
        {
            return source.Host;
        }

        var destination = new Uri(new Uri(UrlHelper.EnsureEndsWithSlash(source.Host)!, UriKind.Absolute),
            "login?apiKey=" + Uri.EscapeDataString(apiKey));
        var token = externalSourceLaunchTokenStore.Issue(destination);
        return $"api/stream/open-external-source?token={Uri.EscapeDataString(token)}";
    }

    private static void MaskExternalSource(ExternalSourceDto source)
    {
        if (!string.IsNullOrEmpty(source.ApiKey)) source.ApiKey = ServerSettingCredentialScopes.MaskedValue;
        source.LaunchApiPath = string.Empty;
    }

    public async Task DeleteExternalSource(int userId, int externalSourceId, CancellationToken ct = default)
    {
        var source = await unitOfWork.AppUserExternalSourceRepository.GetById(externalSourceId, ct);
        if (source == null) throw new LibrariannException("external-source-doesnt-exist");
        if (source.AppUserId != userId) throw new LibrariannException("external-source-doesnt-exist");

        unitOfWork.AppUserExternalSourceRepository.Delete(source);

        // Find all SideNav's with this source and delete them as well
        var streams2 = await unitOfWork.UserRepository.GetSideNavStreamWithExternalSource(externalSourceId, ct);
        unitOfWork.UserRepository.Delete(streams2);

        await unitOfWork.CommitAsync(ct);
    }

    public async Task DeleteSideNavSmartFilterStream(int userId, int sideNavStreamId, CancellationToken ct = default)
    {
        try
        {
            var stream = await unitOfWork.UserRepository.GetSideNavStream(sideNavStreamId, ct);
            if (stream == null) throw new LibrariannException("sidenav-stream-doesnt-exist");

            if (stream.AppUserId != userId) throw new LibrariannException("sidenav-stream-doesnt-exist");


            if (stream.StreamType != SideNavStreamType.SmartFilter)
            {
                throw new LibrariannException("sidenav-stream-only-delete-smart-filter");
            }

            unitOfWork.UserRepository.Delete(stream);

            await unitOfWork.CommitAsync(ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "There was an exception deleting SideNav Smart Filter Stream: {FilterId}", sideNavStreamId);
            throw;
        }
    }

    public async Task DeleteDashboardSmartFilterStream(int userId, int dashboardStreamId, CancellationToken ct = default)
    {
        try
        {
            var stream = await unitOfWork.UserRepository.GetDashboardStream(dashboardStreamId, ct);
            if (stream == null) throw new LibrariannException("dashboard-stream-doesnt-exist");

            if (stream.AppUserId != userId) throw new LibrariannException("dashboard-stream-doesnt-exist");

            if (stream.StreamType != DashboardStreamType.SmartFilter)
            {
                throw new LibrariannException("dashboard-stream-only-delete-smart-filter");
            }

            unitOfWork.UserRepository.Delete(stream);

            await unitOfWork.CommitAsync(ct);
        } catch (Exception ex)
        {
            logger.LogError(ex, "There was an exception deleting Dashboard Smart Filter Stream: {FilterId}", dashboardStreamId);
            throw;
        }
    }

    public async Task RenameSmartFilterStreams(AppUserSmartFilter smartFilter, CancellationToken ct = default)
    {
        var sideNavStreams = await unitOfWork.UserRepository.GetSideNavStreamWithFilter(smartFilter.Id, ct);
        var dashboardStreams = await unitOfWork.UserRepository.GetDashboardStreamWithFilter(smartFilter.Id, ct);

        foreach (var sideNavStream in sideNavStreams)
        {
            sideNavStream.Name = smartFilter.Name;
        }

        foreach (var dashboardStream in dashboardStreams)
        {
            dashboardStream.Name = smartFilter.Name;
        }

        await unitOfWork.CommitAsync(ct);
    }
}
