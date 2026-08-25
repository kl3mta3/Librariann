using System;
using System.Linq.Expressions;
using Librariann.Models.DTOs.Progress;
using Librariann.Models.Entities.User;

namespace Librariann.Models.Mapping;

/// <summary>
/// Explicit replacement for <c>CreateMap&lt;ClientDevice, ClientDeviceDto&gt;()</c>. The nested
/// <see cref="ClientDeviceDto.CurrentClientInfo"/> conversion is inlined directly (rather than composing
/// <see cref="ClientInfoMapping"/>'s expression into this one) since EF Core's query translator is more reliable
/// with a single flat expression tree than with an invoked/composed one.
/// </summary>
public static class ClientDeviceMapping
{
    public static readonly Expression<Func<ClientDevice, ClientDeviceDto>> ToClientDeviceDtoExpression = d => new ClientDeviceDto
    {
        Id = d.Id,
        FriendlyName = d.FriendlyName,
        UiFingerprint = d.UiFingerprint,
        CurrentClientInfo = new ClientInfoDto
        {
            UserAgent = d.CurrentClientInfo.UserAgent,
            IpAddress = d.CurrentClientInfo.IpAddress,
            AuthType = d.CurrentClientInfo.AuthType,
            ClientType = d.CurrentClientInfo.ClientType,
            AppVersion = d.CurrentClientInfo.AppVersion,
            Browser = d.CurrentClientInfo.Browser,
            BrowserVersion = d.CurrentClientInfo.BrowserVersion,
            Platform = d.CurrentClientInfo.Platform,
            DeviceType = d.CurrentClientInfo.DeviceType,
            ScreenWidth = d.CurrentClientInfo.ScreenWidth,
            ScreenHeight = d.CurrentClientInfo.ScreenHeight,
            Orientation = d.CurrentClientInfo.Orientation,
        },
        FirstSeenUtc = d.FirstSeenUtc,
        LastSeenUtc = d.LastSeenUtc,
        OwnerUserId = d.AppUserId,
        OwnerUsername = d.AppUser.UserName!,
    };

    private static readonly Func<ClientDevice, ClientDeviceDto> CompiledToClientDeviceDto = ToClientDeviceDtoExpression.Compile();

    public static ClientDeviceDto ToClientDeviceDto(this ClientDevice d) => CompiledToClientDeviceDto(d);
}
