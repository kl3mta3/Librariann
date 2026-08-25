using System;
using System.Reflection;

namespace Librariann.Common.EnvironmentInfo;

public static class BuildInfo
{
    public static readonly Version Version = Assembly.GetExecutingAssembly().GetName().Version;
    public const string AppName = "Librariann";
    public const string JwtIssuer = AppName;
    public const string JwtAudience = "Librariann.Api";

}
