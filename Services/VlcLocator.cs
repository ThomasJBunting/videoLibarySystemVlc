using Microsoft.Win32;

namespace VideoLibrarySystemVlc.Services;

public sealed class VlcLocator
{
    private string? configuredPath;

    public VlcLocator(string? configuredPath)
    {
        this.configuredPath = configuredPath;
    }

    public void SetConfiguredPath(string? path)
    {
        configuredPath = path;
    }

    public string? Resolve()
    {
        if (!string.IsNullOrWhiteSpace(configuredPath) && File.Exists(configuredPath))
        {
            return configuredPath;
        }

        return Discover();
    }

    public string? Discover()
    {
        foreach (var candidate in EnumerateCandidates())
        {
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        return null;
    }

    private static IEnumerable<string> EnumerateCandidates()
    {
        var commonPaths = new[]
        {
            @"C:\Program Files\VideoLAN\VLC\vlc.exe",
            @"C:\Program Files (x86)\VideoLAN\VLC\vlc.exe"
        };

        foreach (var path in commonPaths)
        {
            yield return path;
        }

        foreach (var path in EnumerateRegistryCandidates())
        {
            yield return path;
        }

        foreach (var path in EnumeratePathCandidates())
        {
            yield return path;
        }
    }

    private static IEnumerable<string> EnumerateRegistryCandidates()
    {
        var views = new[] { RegistryView.Registry64, RegistryView.Registry32 };
        var uninstallSubKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall";

        foreach (var view in views)
        {
            using var baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, view);
            using var uninstallKey = baseKey.OpenSubKey(uninstallSubKey);
            if (uninstallKey is null)
            {
                continue;
            }

            foreach (var subKeyName in uninstallKey.GetSubKeyNames())
            {
                using var subKey = uninstallKey.OpenSubKey(subKeyName);
                if (subKey is null)
                {
                    continue;
                }

                var displayName = subKey.GetValue("DisplayName") as string;
                if (string.IsNullOrWhiteSpace(displayName) || !displayName.Contains("VLC", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var installLocation = subKey.GetValue("InstallLocation") as string;
                if (!string.IsNullOrWhiteSpace(installLocation))
                {
                    yield return Path.Combine(installLocation, "vlc.exe");
                }

                var displayIcon = subKey.GetValue("DisplayIcon") as string;
                if (!string.IsNullOrWhiteSpace(displayIcon))
                {
                    yield return displayIcon.Split(',')[0].Trim('"');
                }
            }
        }
    }

    private static IEnumerable<string> EnumeratePathCandidates()
    {
        var path = Environment.GetEnvironmentVariable("PATH");
        if (string.IsNullOrWhiteSpace(path))
        {
            yield break;
        }

        foreach (var segment in path.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
        {
            yield return Path.Combine(segment.Trim('"'), "vlc.exe");
        }
    }
}
