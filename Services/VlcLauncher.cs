using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using VideoLibrarySystemVlc.Models;

namespace VideoLibrarySystemVlc.Services;

public sealed class VlcLauncher
{
    private readonly VlcLocator locator;

    public VlcLauncher(VlcLocator locator)
    {
        this.locator = locator;
    }

    public Task<VlcSession> LaunchAsync(
        MediaItem item,
        PlaybackSnapshot playback,
        string? startFilePath = null,
        IReadOnlyList<string>? playlistOverride = null,
        CancellationToken cancellationToken = default)
    {
        var vlcPath = locator.Resolve();
        if (string.IsNullOrWhiteSpace(vlcPath))
        {
            throw new InvalidOperationException("VLC could not be found. Use Settings to browse to vlc.exe.");
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = vlcPath,
            UseShellExecute = true,
            WindowStyle = ProcessWindowStyle.Minimized,
            WorkingDirectory = Path.GetDirectoryName(vlcPath) ?? AppPaths.BaseFolder
        };

        startInfo.ArgumentList.Add("--no-one-instance");
        startInfo.ArgumentList.Add("--play-and-exit");
        startInfo.ArgumentList.Add("--no-video-title-show");
        startInfo.ArgumentList.Add("--fullscreen");
        startInfo.ArgumentList.Add("--extraintf=rc");
        if (playback.LastItemId == item.Id && playback.LastKnownTimeSeconds is > 0)
        {
            startInfo.ArgumentList.Add($"--start-time={playback.LastKnownTimeSeconds.Value}");
        }

        if (item.Kind == MediaKind.Series && playlistOverride is not null)
        {
            foreach (var file in playlistOverride)
            {
                startInfo.ArgumentList.Add(file);
            }
        }
        else if (item.Kind == MediaKind.Series && string.IsNullOrWhiteSpace(startFilePath))
        {
            foreach (var file in item.PlaylistPaths)
            {
                startInfo.ArgumentList.Add(file);
            }
        }
        else
        {
            startInfo.ArgumentList.Add(startFilePath ?? item.PrimaryPath);
        }

        var process = Process.Start(startInfo) ?? throw new InvalidOperationException("Unable to start VLC.");
        return Task.FromResult(new VlcSession(process));
    }
}

public sealed class VlcSession : IDisposable
{
    private readonly Process process;

    public VlcSession(Process process)
    {
        this.process = process;
    }

    public void Dispose()
    {
        if (!process.HasExited)
        {
            process.CloseMainWindow();
        }
    }

    public bool TryGetWindowTitle(out string title)
    {
        title = string.Empty;
        if (process.HasExited)
        {
            return false;
        }

        process.Refresh();
        if (string.IsNullOrWhiteSpace(process.MainWindowTitle))
        {
            var candidate = EnumerateProcessWindowTitles(process.Id)
                .OrderByDescending(x => IsLikelyVlcMediaTitle(x))
                .ThenByDescending(x => x.Length)
                .FirstOrDefault();
            if (string.IsNullOrWhiteSpace(candidate))
            {
                return false;
            }

            title = candidate;
            return true;
        }

        title = process.MainWindowTitle;
        return true;
    }

    private static int IsLikelyVlcMediaTitle(string title)
    {
        if (title.Contains(" - VLC media player", StringComparison.OrdinalIgnoreCase))
        {
            return 2;
        }

        if (title.Contains("VLC media player", StringComparison.OrdinalIgnoreCase))
        {
            return 1;
        }

        return 0;
    }

    private static List<string> EnumerateProcessWindowTitles(int processId)
    {
        var titles = new List<string>();
        NativeMethods.EnumWindows((hWnd, _) =>
        {
            NativeMethods.GetWindowThreadProcessId(hWnd, out var windowProcessId);
            if (windowProcessId != processId || !NativeMethods.IsWindowVisible(hWnd))
            {
                return true;
            }

            var length = NativeMethods.GetWindowTextLengthW(hWnd);
            if (length <= 0)
            {
                return true;
            }

            var builder = new StringBuilder(length + 1);
            _ = NativeMethods.GetWindowTextW(hWnd, builder, builder.Capacity);
            var text = builder.ToString().Trim();
            if (!string.IsNullOrWhiteSpace(text))
            {
                titles.Add(text);
            }

            return true;
        }, IntPtr.Zero);

        return titles;
    }

    private static class NativeMethods
    {
        internal delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        internal static extern int GetWindowTextW(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        internal static extern int GetWindowTextLengthW(IntPtr hWnd);

        [DllImport("user32.dll")]
        internal static extern uint GetWindowThreadProcessId(IntPtr hWnd, out int lpdwProcessId);
    }
}
