using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Librariann.Common.EnvironmentInfo;
using Librariann.Common.Helpers;
using Microsoft.Extensions.Hosting;

namespace Librariann.Common;

public static class Configuration
{
    public const string DefaultIpAddresses = "0.0.0.0,::";
    public const string DefaultBaseUrl = "/";
    public const int DefaultHttpPort = 5000;
    public const int DefaultTimeOutSecs = 90;
    public const long DefaultCacheMemory = 75;
    public const string DefaultOidcAuthority = "";
    public const string DefaultOidcClientId = "librariann";
    private static readonly string AppSettingsFilename = Path.Join("config", GetAppSettingFilename());

    public static readonly string LibrariannPlusApiUrl = GetLibrariannPlusApiUrl();


    private static string GetLibrariannPlusApiUrl()
    {
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        var isDevelopment = environment == Environments.Development;

        if (isDevelopment && string.IsNullOrEmpty(Environment.GetEnvironmentVariable("LIBRARIANNPLUS_PROD")))
        {
            return "http://localhost:5020";
        }

        return "https://plus.librariannreader.com";
    }

    public static int Port
    {
        get => GetPort(GetAppSettingFilename());
        set => SetPort(GetAppSettingFilename(), value);
    }

    public static string IpAddresses
    {
        get => GetIpAddresses(GetAppSettingFilename());
        set => SetIpAddresses(GetAppSettingFilename(), value);
    }

    public static string JwtToken
    {
        get => GetJwtToken(GetAppSettingFilename());
        set => SetJwtToken(GetAppSettingFilename(), value);
    }

    public static string BaseUrl
    {
        get => GetBaseUrl(GetAppSettingFilename());
        set => SetBaseUrl(GetAppSettingFilename(), value);
    }

    public static long CacheSize
    {
        get => GetCacheSize(GetAppSettingFilename());
        set => SetCacheSize(GetAppSettingFilename(), value);
    }

    /// <remarks>You must set this object to update the settings, setting one if it's fields will not save to disk</remarks>
    public static OpenIdConnectSettings OidcSettings
    {
        get => GetOpenIdConnectSettings(GetAppSettingFilename());
        set => SetOpenIdConnectSettings(GetAppSettingFilename(), value);
    }

    public static bool AllowIFraming => GetAllowIFraming(GetAppSettingFilename());
    public static IReadOnlyCollection<string> EmbeddingOrigins => GetEmbeddingOrigins(GetAppSettingFilename());
    /// <summary>Premium hosted services are not available in Librariann.</summary>
    public static bool PremiumHostedServicesEnabled => false;
    /// <summary>
    /// Allows an explicitly configured OIDC authority to resolve to private/loopback addresses. Link-local,
    /// metadata-service, unspecified, and multicast addresses remain blocked.
    /// </summary>
    public static bool OidcPrivateNetworkEnabled => GetBooleanOverride(
        "LIBRARIANN_ALLOW_PRIVATE_OIDC_AUTHORITY", GetAppSettingFilename(),
        settings => settings.AllowPrivateOidcAuthority);

    private static string GetAppSettingFilename()
    {
        if (!string.IsNullOrEmpty(AppSettingsFilename))
        {
            return AppSettingsFilename;
        }

        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        var isDevelopment = environment == Environments.Development;
        return "appsettings" + (isDevelopment ? ".Development" : string.Empty) + ".json";
    }

    #region JWT Token

    private static string GetJwtToken(string filePath)
    {
        try
        {
            var json = File.ReadAllText(filePath);
            var jsonObj = JsonSerializer.Deserialize<AppSettings>(json);
            return jsonObj.TokenKey;
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error reading app settings: " + ex.Message);
        }

        return string.Empty;
    }

    private static void SetJwtToken(string filePath, string token)
    {
        try
        {
            var json = File.ReadAllText(filePath);
            var jsonObj = JsonSerializer.Deserialize<AppSettings>(json);
            jsonObj.TokenKey = token;
            json = JsonSerializer.Serialize(jsonObj, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(filePath, json);
        }
        catch (Exception)
        {
            /* Swallow exception */
        }
    }

    public static bool CheckIfJwtTokenSet()
    {
        try
        {
            return !GetJwtToken(GetAppSettingFilename()).StartsWith("super secret unguessable key");
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error writing app settings: " + ex.Message);
        }

        return false;
    }

    #endregion

    private static bool GetBooleanOverride(string environmentVariable, string filePath,
        Func<AppSettings, bool> selector)
    {
        if (bool.TryParse(Environment.GetEnvironmentVariable(environmentVariable), out var environmentValue))
            return environmentValue;

        try
        {
            var json = File.ReadAllText(filePath);
            var settings = JsonSerializer.Deserialize<AppSettings>(json);
            return settings != null && selector(settings);
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error reading boolean application setting: " + ex.Message);
            return false;
        }
    }

    #region Embedding Origins
    private static IReadOnlyCollection<string> GetEmbeddingOrigins(string filePath)
    {
        try
        {
            var json = File.ReadAllText(filePath);
            var jsonObj = JsonSerializer.Deserialize<AppSettings>(json);
            return jsonObj?.EmbeddingOrigins ?? [];
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error reading embedding origins: " + ex.Message);
        }

        return [];
    }
    #endregion

    #region Port

    private static void SetPort(string filePath, int port)
    {
        if (OsInfo.IsDocker)
        {
            return;
        }

        try
        {
            var json = File.ReadAllText(filePath);
            var jsonObj = JsonSerializer.Deserialize<AppSettings>(json);
            jsonObj.Port = port;
            json = JsonSerializer.Serialize(jsonObj, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(filePath, json);
        }
        catch (Exception)
        {
            /* Swallow Exception */
        }
    }

    private static int GetPort(string filePath)
    {
        if (OsInfo.IsDocker)
        {
            return DefaultHttpPort;
        }

        try
        {
            var json = File.ReadAllText(filePath);
            var jsonObj = JsonSerializer.Deserialize<AppSettings>(json);
            return jsonObj.Port;
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error writing app settings: " + ex.Message);
        }

        return DefaultHttpPort;
    }

    #endregion

    #region Ip Addresses

    private static void SetIpAddresses(string filePath, string ipAddresses)
    {
        if (OsInfo.IsDocker)
        {
            return;
        }

        try
        {
            var json = File.ReadAllText(filePath);
            var jsonObj = JsonSerializer.Deserialize<AppSettings>(json);
            jsonObj.IpAddresses = ipAddresses;
            json = JsonSerializer.Serialize(jsonObj, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(filePath, json);
        }
        catch (Exception)
        {
            /* Swallow Exception */
        }
    }

    private static string GetIpAddresses(string filePath)
    {
        if (OsInfo.IsDocker)
        {
            return string.Empty;
        }

        try
        {
            var json = File.ReadAllText(filePath);
            var jsonObj = JsonSerializer.Deserialize<AppSettings>(json);
            return jsonObj.IpAddresses;
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error writing app settings: " + ex.Message);
        }

        return string.Empty;
    }
    #endregion

    #region BaseUrl
    private static string GetBaseUrl(string filePath)
    {
        try
        {
            var json = File.ReadAllText(filePath);
            var jsonObj = JsonSerializer.Deserialize<AppSettings>(json);

            var baseUrl = jsonObj.BaseUrl;
            if (!string.IsNullOrEmpty(baseUrl))
            {
                baseUrl = UrlHelper.EnsureStartsWithSlash(baseUrl);
                baseUrl = UrlHelper.EnsureEndsWithSlash(baseUrl);

                return baseUrl;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error reading app settings: " + ex.Message);
        }

        return DefaultBaseUrl;
    }

    private static void SetBaseUrl(string filePath, string value)
    {

        var baseUrl = !value.StartsWith('/')
            ? $"/{value}"
            : value;

        baseUrl = !baseUrl.EndsWith('/')
                    ? $"{baseUrl}/"
                    : baseUrl;

        try
        {
            var json = File.ReadAllText(filePath);
            var jsonObj = JsonSerializer.Deserialize<AppSettings>(json);
            jsonObj.BaseUrl = baseUrl;
            json = JsonSerializer.Serialize(jsonObj, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(filePath, json);
        }
        catch (Exception)
        {
            /* Swallow exception */
        }
    }
    #endregion

    #region CacheSize
    private static void SetCacheSize(string filePath, long cache)
    {
        if (cache <= 0) return;
        try
        {
            var json = File.ReadAllText(filePath);
            var jsonObj = JsonSerializer.Deserialize<AppSettings>(json);
            jsonObj.Cache = cache;
            json = JsonSerializer.Serialize(jsonObj, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(filePath, json);
        }
        catch (Exception)
        {
            /* Swallow Exception */
        }
    }

    private static long GetCacheSize(string filePath)
    {
        try
        {
            var json = File.ReadAllText(filePath);
            var jsonObj = JsonSerializer.Deserialize<AppSettings>(json);

            return jsonObj.Cache == 0 ? DefaultCacheMemory : jsonObj.Cache;
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error writing app settings: " + ex.Message);
        }

        return DefaultCacheMemory;
    }


    #endregion

    #region AllowIFraming
    private static bool GetAllowIFraming(string filePath)
    {
        try
        {
            var json = File.ReadAllText(filePath);
            var jsonObj = JsonSerializer.Deserialize<AppSettings>(json);
            return jsonObj.AllowIFraming;
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error reading app settings: " + ex.Message);
        }

        return false;
    }
    #endregion

    #region OIDC

    private static OpenIdConnectSettings GetOpenIdConnectSettings(string filePath)
    {
        try
        {
            var json = File.ReadAllText(filePath);
            var jsonObj = JsonSerializer.Deserialize<AppSettings>(json);

            return jsonObj.OpenIdConnectSettings ?? new OpenIdConnectSettings();
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error reading app settings: " + ex.Message);
        }

        return new OpenIdConnectSettings();
    }

    private static void SetOpenIdConnectSettings(string filePath, OpenIdConnectSettings value)
    {
        try
        {
            var json = File.ReadAllText(filePath);
            var jsonObj = JsonSerializer.Deserialize<AppSettings>(json);
            jsonObj.OpenIdConnectSettings = value;
            json = JsonSerializer.Serialize(jsonObj, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(filePath, json);
        }
        catch (Exception)
        {
            /* Swallow exception */
        }
    }

    #endregion

    private sealed class AppSettings
    {
        public string TokenKey { get; set; }
        // ReSharper disable once MemberHidesStaticFromOuterClass
#pragma warning disable S3218
        public int Port { get; set; } = DefaultHttpPort;
        // ReSharper disable once MemberHidesStaticFromOuterClass
        public string IpAddresses { get; set; } = string.Empty;
        // ReSharper disable once MemberHidesStaticFromOuterClass
        public string BaseUrl { get; set; }
        // ReSharper disable once MemberHidesStaticFromOuterClass
        public long Cache { get; set; } = DefaultCacheMemory;
        // ReSharper disable once MemberHidesStaticFromOuterClass
        public bool AllowIFraming { get; init; } = false;
        public bool AllowPrivateOidcAuthority { get; init; } = false;
        public List<string> EmbeddingOrigins { get; init; } = [];
        public OpenIdConnectSettings OpenIdConnectSettings { get; set; } = new();
        [JsonExtensionData]
        public Dictionary<string, JsonElement> ExtensionData { get; set; }
#pragma warning restore S3218
    }

    public class OpenIdConnectSettings
    {
        public string Authority { get; set; } = DefaultOidcAuthority;
        public string ClientId { get; set; } = DefaultOidcClientId;
        public string Secret { get; set; } = string.Empty;
        public List<string> CustomScopes { get; set; } = [];

        public bool Enabled =>
            !string.IsNullOrEmpty(Authority) &&
            !string.IsNullOrEmpty(ClientId) &&
            !string.IsNullOrEmpty(Secret);
    }
}
