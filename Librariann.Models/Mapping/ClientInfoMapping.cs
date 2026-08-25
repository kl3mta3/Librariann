using Librariann.Models.DTOs.Progress;
using Librariann.Models.Entities.Progress;

namespace Librariann.Models.Mapping;

/// <summary>Explicit replacement for <c>CreateMap&lt;ClientInfoData, ClientInfoDto&gt;()</c>.</summary>
public static class ClientInfoMapping
{
    public static ClientInfoDto ToClientInfoDto(this ClientInfoData c) => new()
    {
        UserAgent = c.UserAgent,
        IpAddress = c.IpAddress,
        AuthType = c.AuthType,
        ClientType = c.ClientType,
        AppVersion = c.AppVersion,
        Browser = c.Browser,
        BrowserVersion = c.BrowserVersion,
        Platform = c.Platform,
        DeviceType = c.DeviceType,
        ScreenWidth = c.ScreenWidth,
        ScreenHeight = c.ScreenHeight,
        Orientation = c.Orientation,
    };
}
