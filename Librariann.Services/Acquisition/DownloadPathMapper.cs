using System;
using System.IO;
using Librariann.Models.Entities.Acquisition;

namespace Librariann.Services.Acquisition;

public static class DownloadPathMapper
{
    public static bool TryMap(IntegrationProviderConfiguration configuration, string remoteOutputPath, out string localPath)
    {
        localPath = string.Empty;
        if (string.IsNullOrWhiteSpace(configuration.RemotePath) || string.IsNullOrWhiteSpace(configuration.LocalPath) ||
            string.IsNullOrWhiteSpace(remoteOutputPath)) return false;

        var remoteRoot = NormalizeRemote(configuration.RemotePath).TrimEnd('/');
        var output = NormalizeRemote(remoteOutputPath);
        var comparison = LooksLikeWindowsPath(remoteRoot) ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        if (!output.Equals(remoteRoot, comparison) && !output.StartsWith(remoteRoot + "/", comparison)) return false;

        var suffix = output[remoteRoot.Length..].TrimStart('/');
        var localRoot = Path.GetFullPath(configuration.LocalPath);
        var platformSuffix = suffix.Replace('/', Path.DirectorySeparatorChar);
        var candidate = Path.GetFullPath(Path.Combine(localRoot, platformSuffix));
        var relative = Path.GetRelativePath(localRoot, candidate);
        if (Path.IsPathRooted(relative) || relative.Equals("..", StringComparison.Ordinal) ||
            relative.StartsWith(".." + Path.DirectorySeparatorChar, StringComparison.Ordinal)) return false;

        localPath = candidate;
        return true;
    }

    private static string NormalizeRemote(string path) => path.Trim().Replace('\\', '/');
    private static bool LooksLikeWindowsPath(string path) => path.Length >= 2 && char.IsLetter(path[0]) && path[1] == ':';
}
