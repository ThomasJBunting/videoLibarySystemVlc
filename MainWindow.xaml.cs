using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using VideoLibrarySystemVlc.Models;
using VideoLibrarySystemVlc.Services;

namespace VideoLibrarySystemVlc;



public partial class MainWindow : Window, INotifyPropertyChanged
{
    private readonly JsonStateStore stateStore = new();
    private readonly MediaScanner mediaScanner = new();
    private readonly ArtworkResolver artworkResolver = new();
    private readonly VlcLocator vlcLocator;
    private readonly VlcLauncher vlcLauncher;
    private readonly AppState appState;
    private readonly System.Drawing.Icon trayIconImage;
    private readonly NotifyIcon trayIcon;
    private bool exitRequested;
    private VlcSession? currentSession;
    private CancellationTokenSource? sessionTrackCts;
    private string? trackedSeriesItemId;
    private bool canStartPlayback = true;

    private ObservableCollection<LibraryRoot> seriesRoots = [];
    private ObservableCollection<LibraryRoot> movieRoots = [];
    private ObservableCollection<LibraryRoot> seriesRootOptions = [];
    private ObservableCollection<LibraryRoot> movieRootOptions = [];
    private ObservableCollection<MediaItem> seriesItems = [];
    private ObservableCollection<MediaItem> movieItems = [];
    private ObservableCollection<MediaItem> seriesVisibleItems = [];
    private ObservableCollection<MediaItem> movieVisibleItems = [];
    private ObservableCollection<EpisodeDisplayEntry> selectedSeriesEpisodeEntries = [];
    private LibraryRoot? selectedSeriesRoot;
    private LibraryRoot? selectedMovieRoot;
    private MediaItem? selectedSeriesItem;
    private MediaItem? selectedMovieItem;
    private string statusText = "Ready.";
    private string vlcExecutablePath = string.Empty;
    private string vlcPathStatus = string.Empty;
	private bool isDarkMode = false;
	public MainWindow()
    {
        InitializeComponent();
        Icon = IconFactory.CreateWindowIcon();

        appState = stateStore.LoadOrCreate();
        vlcLocator = new VlcLocator(appState.Settings.VlcExecutablePath);
        vlcLauncher = new VlcLauncher(vlcLocator);
        trayIconImage = IconFactory.CreateTrayIcon();

        DataContext = this;
        LoadStateToUi();
        AutoConfigureVlcPath();

        trayIcon = new NotifyIcon
        {
            Icon = trayIconImage,
            Visible = true,
            Text = "Video Library System VLC",
            ContextMenuStrip = BuildTrayMenu()
        };
        trayIcon.DoubleClick += (_, _) => ShowFromTray();

        Loaded += async (_, _) => await RefreshAllAsync();
        Closing += OnClosing;
    }
	private void ToggleDarkMode_Click(object sender, RoutedEventArgs e)
	{
		var appResources = System.Windows.Application.Current.Resources.MergedDictionaries;
		if (isDarkMode)
		{
			// Switch to Light Theme
			appResources.Clear();
			appResources.Add(new ResourceDictionary { Source = new Uri("ResourceDictionaries/LightTheme.xaml", UriKind.Relative) });
			isDarkMode = false;
		}
		else
		{
			// Switch to Dark Theme
			appResources.Clear();
			appResources.Add(new ResourceDictionary { Source = new Uri("ResourceDictionaries/DarkTheme.xaml", UriKind.Relative) });
			isDarkMode = true;
		}
	}

	public ObservableCollection<LibraryRoot> SeriesRoots
    {
        get => seriesRoots;
        set
        {
            SetField(ref seriesRoots, value);
            RefreshRootOptions();
        }
    }

    public ObservableCollection<LibraryRoot> MovieRoots
    {
        get => movieRoots;
        set
        {
            SetField(ref movieRoots, value);
            RefreshRootOptions();
        }
    }

    public ObservableCollection<LibraryRoot> SeriesRootOptions
    {
        get => seriesRootOptions;
        set => SetField(ref seriesRootOptions, value);
    }

    public ObservableCollection<LibraryRoot> MovieRootOptions
    {
        get => movieRootOptions;
        set => SetField(ref movieRootOptions, value);
    }

    public ObservableCollection<MediaItem> SeriesItems
    {
        get => seriesItems;
        set
        {
            SetField(ref seriesItems, value);
            RefreshVisibleItems();
        }
    }

    public ObservableCollection<MediaItem> MovieItems
    {
        get => movieItems;
        set
        {
            SetField(ref movieItems, value);
            RefreshVisibleItems();
        }
    }

    public ObservableCollection<MediaItem> SeriesVisibleItems
    {
        get => seriesVisibleItems;
        set => SetField(ref seriesVisibleItems, value);
    }

    public ObservableCollection<MediaItem> MovieVisibleItems
    {
        get => movieVisibleItems;
        set => SetField(ref movieVisibleItems, value);
    }

    public LibraryRoot? SelectedSeriesRoot
    {
        get => selectedSeriesRoot;
        set
        {
            if (SetField(ref selectedSeriesRoot, value))
            {
                RefreshVisibleItems();
            }
        }
    }

    public LibraryRoot? SelectedMovieRoot
    {
        get => selectedMovieRoot;
        set
        {
            if (SetField(ref selectedMovieRoot, value))
            {
                RefreshVisibleItems();
            }
        }
    }

    public MediaItem? SelectedSeriesItem
    {
        get => selectedSeriesItem;
        set
        {
            if (SetField(ref selectedSeriesItem, value))
            {
                RefreshSelectedSeriesEpisodeEntries();
            }
        }
    }

    public MediaItem? SelectedMovieItem
    {
        get => selectedMovieItem;
        set => SetField(ref selectedMovieItem, value);
    }

    public string StatusText
    {
        get => statusText;
        set => SetField(ref statusText, value);
    }

    public string VlcExecutablePath
    {
        get => vlcExecutablePath;
        set => SetField(ref vlcExecutablePath, value);
    }

    public string VlcPathStatus
    {
        get => vlcPathStatus;
        set => SetField(ref vlcPathStatus, value);
    }

    public bool CanStartPlayback
    {
        get => canStartPlayback;
        set => SetField(ref canStartPlayback, value);
    }

    public ObservableCollection<EpisodeDisplayEntry> SelectedSeriesEpisodeEntries
    {
        get => selectedSeriesEpisodeEntries;
        set => SetField(ref selectedSeriesEpisodeEntries, value);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private ContextMenuStrip BuildTrayMenu()
    {
        var menu = new ContextMenuStrip();
        menu.Items.Add("Open", null, (_, _) => Dispatcher.Invoke(ShowFromTray));
        menu.Items.Add("Scan All", null, async (_, _) => await Dispatcher.InvokeAsync(RefreshAllAsync));
        menu.Items.Add("Exit", null, (_, _) =>
        {
            exitRequested = true;
            Dispatcher.Invoke(Close);
        });
        return menu;
    }

    private void LoadStateToUi()
    {
        SeriesRoots = new ObservableCollection<LibraryRoot>(appState.Settings.LibraryRoots.Where(x => x.Kind == LibraryRootKind.Series));
        MovieRoots = new ObservableCollection<LibraryRoot>(appState.Settings.LibraryRoots.Where(x => x.Kind == LibraryRootKind.Movies));
        SeriesItems = new ObservableCollection<MediaItem>(appState.MediaItems.Where(x => x.Kind == MediaKind.Series));
        MovieItems = new ObservableCollection<MediaItem>(appState.MediaItems.Where(x => x.Kind == MediaKind.Movie));
        AttachPersistenceHandlers(SeriesItems);
        AttachPersistenceHandlers(MovieItems);
        VlcExecutablePath = appState.Settings.VlcExecutablePath ?? string.Empty;
        VlcPathStatus = string.IsNullOrWhiteSpace(VlcExecutablePath)
            ? "VLC path not set yet."
            : $"Current VLC path: {VlcExecutablePath}";
        RefreshRootOptions();
        RefreshVisibleItems();
        RefreshSelectedSeriesEpisodeEntries();
    }

    private void RefreshRootOptions()
    {
        var seriesOptions = new ObservableCollection<LibraryRoot>
        {
            new() { Path = string.Empty, DisplayName = "All series", Kind = LibraryRootKind.Series }
        };
        foreach (var root in SeriesRoots)
        {
            seriesOptions.Add(root);
        }

        var movieOptions = new ObservableCollection<LibraryRoot>
        {
            new() { Path = string.Empty, DisplayName = "All movies", Kind = LibraryRootKind.Movies }
        };
        foreach (var root in MovieRoots)
        {
            movieOptions.Add(root);
        }

        SeriesRootOptions = seriesOptions;
        MovieRootOptions = movieOptions;

        var selectedSeriesPath = SelectedSeriesRoot?.Path;
        SelectedSeriesRoot = string.IsNullOrWhiteSpace(selectedSeriesPath)
            ? SeriesRootOptions.FirstOrDefault()
            : SeriesRootOptions.FirstOrDefault(root => string.Equals(root.Path, selectedSeriesPath, StringComparison.OrdinalIgnoreCase)) ?? SeriesRootOptions.FirstOrDefault();

        var selectedMoviePath = SelectedMovieRoot?.Path;
        SelectedMovieRoot = string.IsNullOrWhiteSpace(selectedMoviePath)
            ? MovieRootOptions.FirstOrDefault()
            : MovieRootOptions.FirstOrDefault(root => string.Equals(root.Path, selectedMoviePath, StringComparison.OrdinalIgnoreCase)) ?? MovieRootOptions.FirstOrDefault();
    }

    private void RefreshVisibleItems()
    {
        SeriesVisibleItems = new ObservableCollection<MediaItem>(SeriesItems.Where(item => IsVisibleForRoot(item, SelectedSeriesRoot)));
        MovieVisibleItems = new ObservableCollection<MediaItem>(MovieItems.Where(item => IsVisibleForRoot(item, SelectedMovieRoot)));

        if (SelectedSeriesItem is not null && !SeriesVisibleItems.Contains(SelectedSeriesItem))
        {
            SelectedSeriesItem = SeriesVisibleItems.FirstOrDefault();
        }

        if (SelectedMovieItem is not null && !MovieVisibleItems.Contains(SelectedMovieItem))
        {
            SelectedMovieItem = MovieVisibleItems.FirstOrDefault();
        }
    }

    private static bool IsVisibleForRoot(MediaItem item, LibraryRoot? selectedRoot)
    {
        if (selectedRoot is null || string.IsNullOrWhiteSpace(selectedRoot.Path))
        {
            return true;
        }

        return string.Equals(item.RootPath, selectedRoot.Path, StringComparison.OrdinalIgnoreCase);
    }

    private void AutoConfigureVlcPath()
    {
        if (!string.IsNullOrWhiteSpace(VlcExecutablePath) && File.Exists(VlcExecutablePath))
        {
            vlcLocator.SetConfiguredPath(VlcExecutablePath);
            return;
        }

        var discovered = vlcLocator.Discover();
        if (discovered is null)
        {
            VlcPathStatus = "VLC was not found automatically. Use Settings to browse to vlc.exe.";
            return;
        }

        VlcExecutablePath = discovered;
        VlcPathStatus = $"Detected VLC at {discovered}";
        PersistVlcPath();
    }

    private async Task RefreshAllAsync()
    {
        await RefreshSeriesAsync();
        await RefreshMoviesAsync();
    }

    private async Task RefreshSeriesAsync()
    {
        StatusText = "Scanning series roots...";
        var result = await Task.Run(() =>
        {
            var scan = mediaScanner.Scan(SeriesRoots.ToList(), SeriesItems.ToList().Concat(MovieItems.ToList()).ToList());
            artworkResolver.PopulateArtwork(scan.Items);
            return scan;
        });
        SeriesItems = new ObservableCollection<MediaItem>(result.Items.Where(x => x.Kind == MediaKind.Series));
        AttachPersistenceHandlers(SeriesItems);
        PersistState();
        StatusText = result.SkippedRoots.Count == 0
            ? $"Series scanned: {SeriesItems.Count}"
            : $"Series scanned: {SeriesItems.Count} (skipped {result.SkippedRoots.Count} inaccessible roots)";
    }

    private async Task RefreshMoviesAsync()
    {
        StatusText = "Scanning movie roots...";
        var result = await Task.Run(() =>
        {
            var scan = mediaScanner.Scan(MovieRoots.ToList(), SeriesItems.ToList().Concat(MovieItems.ToList()).ToList());
            artworkResolver.PopulateArtwork(scan.Items);
            return scan;
        });
        MovieItems = new ObservableCollection<MediaItem>(result.Items.Where(x => x.Kind == MediaKind.Movie));
        AttachPersistenceHandlers(MovieItems);
        PersistState();
        StatusText = result.SkippedRoots.Count == 0
            ? $"Movies scanned: {MovieItems.Count}"
            : $"Movies scanned: {MovieItems.Count} (skipped {result.SkippedRoots.Count} inaccessible roots)";
    }

    private void PersistState()
    {
        appState.Settings.LibraryRoots = SeriesRoots.Concat(MovieRoots).ToList();
        appState.MediaItems = SeriesItems.Concat(MovieItems).ToList();
        stateStore.Save(appState);
    }

    private void OnClosing(object? sender, CancelEventArgs e)
    {
        if (exitRequested)
        {
            trayIcon.Visible = false;
            trayIcon.Dispose();
            trayIconImage.Dispose();
            currentSession?.Dispose();
            PersistState();
            return;
        }

        e.Cancel = true;
        Hide();
        trayIcon.ShowBalloonTip(1000, "Video Library System VLC", "Still running in the tray.", ToolTipIcon.Info);
    }

    private void ShowFromTray()
    {
        Show();
        WindowState = WindowState.Normal;
        Activate();
    }

    private void AddSeriesRoot_Click(object sender, RoutedEventArgs e) => AddRoot(LibraryRootKind.Series);
    private void AddMovieRoot_Click(object sender, RoutedEventArgs e) => AddRoot(LibraryRootKind.Movies);

    private void AddRoot(LibraryRootKind kind)
    {
        using var dialog = new FolderBrowserDialog
        {
            Description = kind == LibraryRootKind.Series ? "Choose a series root folder" : "Choose a movie root folder"
        };

        if (dialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
        {
            return;
        }

        var collection = kind == LibraryRootKind.Series ? SeriesRoots : MovieRoots;
        if (collection.Any(root => string.Equals(root.Path, dialog.SelectedPath, StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        collection.Add(new LibraryRoot
        {
            Path = dialog.SelectedPath,
            DisplayName = Path.GetFileName(dialog.SelectedPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)),
            Kind = kind
        });

        RefreshRootOptions();
        PersistState();
    }

    private void BrowseVlcPath_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "vlc.exe|vlc.exe|Executable Files|*.exe|All Files|*.*",
            FileName = "vlc.exe"
        };

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        VlcExecutablePath = dialog.FileName;
        VlcPathStatus = $"Selected {dialog.FileName}";
    }

    private void DetectVlcPath_Click(object sender, RoutedEventArgs e)
    {
        var discovered = vlcLocator.Discover();
        if (discovered is null)
        {
            VlcPathStatus = "VLC still could not be found automatically.";
            return;
        }

        VlcExecutablePath = discovered;
        VlcPathStatus = $"Detected VLC at {discovered}";
    }

    private void SaveVlcPath_Click(object sender, RoutedEventArgs e)
    {
        PersistVlcPath();
    }

    private void PersistVlcPath()
    {
        var path = VlcExecutablePath.Trim();
        appState.Settings.VlcExecutablePath = string.IsNullOrWhiteSpace(path) ? null : path;
        vlcLocator.SetConfiguredPath(appState.Settings.VlcExecutablePath);
        stateStore.Save(appState);
        VlcPathStatus = string.IsNullOrWhiteSpace(appState.Settings.VlcExecutablePath)
            ? "VLC path cleared."
            : $"Saved VLC path: {appState.Settings.VlcExecutablePath}";
    }

    private void RemoveSeriesRoot_Click(object sender, RoutedEventArgs e) => RemoveRoot(SeriesRoots, SelectedSeriesRoot);
    private void RemoveMovieRoot_Click(object sender, RoutedEventArgs e) => RemoveRoot(MovieRoots, SelectedMovieRoot);

    private void RemoveRoot(ObservableCollection<LibraryRoot> collection, LibraryRoot? selected)
    {
        if (selected is null || string.IsNullOrWhiteSpace(selected.Path))
        {
            return;
        }

        collection.Remove(selected);

        SeriesItems = new ObservableCollection<MediaItem>(SeriesItems.Where(item => !string.Equals(item.RootPath, selected.Path, StringComparison.OrdinalIgnoreCase)));
        MovieItems = new ObservableCollection<MediaItem>(MovieItems.Where(item => !string.Equals(item.RootPath, selected.Path, StringComparison.OrdinalIgnoreCase)));
        if (string.Equals(selected.Path, SelectedSeriesRoot?.Path, StringComparison.OrdinalIgnoreCase))
        {
            SelectedSeriesRoot = SeriesRootOptions.FirstOrDefault();
        }

        if (string.Equals(selected.Path, SelectedMovieRoot?.Path, StringComparison.OrdinalIgnoreCase))
        {
            SelectedMovieRoot = MovieRootOptions.FirstOrDefault();
        }

        RefreshRootOptions();
        RefreshVisibleItems();
        PersistState();
    }

    private async void ScanSeries_Click(object sender, RoutedEventArgs e) => await RefreshSeriesAsync();
    private async void ScanMovies_Click(object sender, RoutedEventArgs e) => await RefreshMoviesAsync();

    private async void PlayMovie_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.DataContext is MediaItem item)
        {
            await PlayItemAsync(item, null);
        }
    }

    private async void PlaySelectedMovie_Click(object sender, RoutedEventArgs e)
    {
        if (SelectedMovieItem is not null)
        {
            await PlayItemAsync(SelectedMovieItem, null);
        }
    }

    private async void PlaySeriesPlaylist_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.DataContext is MediaItem item)
        {
            await PlayItemAsync(item, null);
        }
    }

    private async void ResumeSeriesPlaylist_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.DataContext is MediaItem item)
        {
            await PlaySeriesFromResumePointAsync(item);
        }
    }

    private async void ResumeSameVideo_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.DataContext is MediaItem item)
        {
            await ResumeSameVideoAsync(item);
        }
    }

    private async void PlaySelectedSeriesPlaylist_Click(object sender, RoutedEventArgs e)
    {
        if (SelectedSeriesItem is not null)
        {
            await PlayItemAsync(SelectedSeriesItem, null);
        }
    }

    private async void ResumeSelectedSeriesPlaylist_Click(object sender, RoutedEventArgs e)
    {
        if (SelectedSeriesItem is not null)
        {
            await PlaySeriesFromResumePointAsync(SelectedSeriesItem);
        }
    }

    private async void PlaySelectedSeriesEpisode_Click(object sender, RoutedEventArgs e)
    {
        await PlaySelectedSeriesEpisodeAsync();
    }

    private async void SeriesEpisodeList_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        await PlaySelectedSeriesEpisodeAsync();
    }

    private async Task PlaySelectedSeriesEpisodeAsync()
    {
        if (SelectedSeriesItem is null)
        {
            return;
        }

        if (SeriesEpisodeList.SelectedItem is not EpisodeDisplayEntry selectedEpisode || string.IsNullOrWhiteSpace(selectedEpisode.FilePath))
        {
            return;
        }

        await PlayItemAsync(SelectedSeriesItem, selectedEpisode.FilePath);
    }

    private void SetSelectedSeriesArtwork_Click(object sender, RoutedEventArgs e) => SetArtwork(SelectedSeriesItem);
    private void SetSelectedMovieArtwork_Click(object sender, RoutedEventArgs e) => SetArtwork(SelectedMovieItem);

    private void SetArtwork(MediaItem? item)
    {
        if (item is null)
        {
            return;
        }

        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "Image Files|*.png;*.jpg;*.jpeg;*.bmp;*.gif|All Files|*.*"
        };

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        item.ArtworkPath = dialog.FileName;
        PersistState();
    }

    private async Task PlayItemAsync(
        MediaItem item,
        string? explicitEpisodePath,
        IReadOnlyList<string>? playlistOverride = null,
        string? overridePlayedPath = null)
    {
        if (!CanStartPlayback)
        {
            return;
        }

        if (!EnsureVlcPathAvailable())
        {
            return;
        }

        CanStartPlayback = false;
        sessionTrackCts?.Cancel();
        sessionTrackCts?.Dispose();
        sessionTrackCts = null;
        trackedSeriesItemId = null;
        currentSession?.Dispose();

        var launchSucceeded = false;
        try
        {
            var snapshot = appState.Playback;
            currentSession = await vlcLauncher.LaunchAsync(item, snapshot, explicitEpisodePath, playlistOverride);
            var playedFilePath = overridePlayedPath ?? ResolvePlayedPath(item, explicitEpisodePath);
            appState.Playback.LastItemId = item.Id;
            appState.Playback.LastFilePath = playedFilePath;
            appState.Playback.LastStartedUtc = DateTime.UtcNow;
            appState.Playback.LastKnownTimeSeconds = null;
            UpdateRecentSeriesPlays(item, playedFilePath);
            stateStore.Save(appState);
            RefreshSelectedSeriesEpisodeEntries();
            if (item.Kind == MediaKind.Series && currentSession is not null)
            {
                trackedSeriesItemId = item.Id;
                sessionTrackCts = new CancellationTokenSource();
                _ = TrackSeriesPlaybackFromVlcAsync(currentSession, item.Id, sessionTrackCts.Token);
            }
            StatusText = $"Playing {item.Title}";
            launchSucceeded = true;
            _ = ReleasePlaybackGuardAsync();
        }
        catch (InvalidOperationException)
        {
            VlcPathStatus = "VLC could not be launched. Check the path in Settings.";
            StatusText = "Playback failed.";
        }
        finally
        {
            if (!launchSucceeded)
            {
                CanStartPlayback = true;
            }
        }
    }

    private async Task PlaySeriesFromResumePointAsync(MediaItem item)
    {
        if (item.Kind != MediaKind.Series || item.PlaylistPaths.Count == 0)
        {
            return;
        }

        var lastPlayedPath = appState.Playback.RecentSeriesPlays
            .Where(x => string.Equals(x.SeriesItemId, item.Id, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(x => x.PlayedUtc)
            .Select(x => x.EpisodePath)
            .FirstOrDefault();

        var startIndex = 0;
        if (!string.IsNullOrWhiteSpace(lastPlayedPath))
        {
            var lastIndex = item.PlaylistPaths.FindIndex(path => string.Equals(path, lastPlayedPath, StringComparison.OrdinalIgnoreCase));
            if (lastIndex >= 0)
            {
                startIndex = Math.Min(lastIndex + 1, item.PlaylistPaths.Count - 1);
            }
        }

        var resumePlaylist = item.PlaylistPaths.Skip(startIndex).ToList();
        if (resumePlaylist.Count == 0)
        {
            return;
        }

        await PlayItemAsync(item, null, resumePlaylist, resumePlaylist[0]);
    }

    private async Task ResumeSameVideoAsync(MediaItem item)
    {
        if (item.Kind != MediaKind.Series || item.PlaylistPaths.Count == 0)
        {
            return;
        }

        // Get the last played episode path from recent plays
        var lastPlayedPath = appState.Playback.RecentSeriesPlays
            .Where(x => string.Equals(x.SeriesItemId, item.Id, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(x => x.PlayedUtc)
            .Select(x => x.EpisodePath)
            .FirstOrDefault();

        // Find the index of the last played episode (or start from beginning if never played)
        var startIndex = 0;
        if (!string.IsNullOrWhiteSpace(lastPlayedPath))
        {
            var lastIndex = item.PlaylistPaths.FindIndex(path => string.Equals(path, lastPlayedPath, StringComparison.OrdinalIgnoreCase));
            if (lastIndex >= 0)
            {
                startIndex = lastIndex; // Start from the SAME episode (not next one)
            }
        }

        // Create playlist from the current episode onwards
        var resumePlaylist = item.PlaylistPaths.Skip(startIndex).ToList();
        if (resumePlaylist.Count == 0)
        {
            return;
        }

        // Calculate time offset for the first episode only
        var timeOffsetSeconds = 0;
        if (string.Equals(appState.Playback.LastItemId, item.Id, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(appState.Playback.LastFilePath, resumePlaylist[0], StringComparison.OrdinalIgnoreCase) &&
            appState.Playback.LastStartedUtc.HasValue)
        {
            // Estimate the current position based on elapsed time
            var elapsed = DateTime.UtcNow - appState.Playback.LastStartedUtc.Value;
            var estimatedCurrentSeconds = (int)elapsed.TotalSeconds;

            // Start 60 seconds before the estimated position, but not before 0
            timeOffsetSeconds = Math.Max(0, estimatedCurrentSeconds - 60);
        }

        // If we have a time offset, use XSPF playlist with per-track options
        // This allows start-time ONLY on the first track, not subsequent ones
        if (timeOffsetSeconds > 0)
        {
            await PlayWithXspfPlaylistAsync(item, resumePlaylist, timeOffsetSeconds);
        }
        else
        {
            // No time offset, use normal playlist
            await PlayItemAsync(item, null, resumePlaylist, resumePlaylist[0]);
        }
    }

    private async Task PlayWithXspfPlaylistAsync(MediaItem item, List<string> episodePaths, int startTimeSeconds)
    {
        // Create a temporary XSPF playlist file with start time only for the first track
        var playlistPath = CreateXspfPlaylistWithStartTime(episodePaths, startTimeSeconds);

        if (!EnsureVlcPathAvailable())
        {
            return;
        }

        CanStartPlayback = false;
        sessionTrackCts?.Cancel();
        sessionTrackCts?.Dispose();
        sessionTrackCts = null;
        trackedSeriesItemId = null;
        currentSession?.Dispose();

        var launchSucceeded = false;
        try
        {
            var vlcPath = vlcLocator.Resolve();
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
            startInfo.ArgumentList.Add(playlistPath);

            var process = Process.Start(startInfo) ?? throw new InvalidOperationException("Unable to start VLC.");
            currentSession = new VlcSession(process);

            appState.Playback.LastItemId = item.Id;
            appState.Playback.LastFilePath = episodePaths[0];
            appState.Playback.LastStartedUtc = DateTime.UtcNow;
            appState.Playback.LastKnownTimeSeconds = null;
            UpdateRecentSeriesPlays(item, episodePaths[0]);
            stateStore.Save(appState);
            RefreshSelectedSeriesEpisodeEntries();

            if (item.Kind == MediaKind.Series && currentSession is not null)
            {
                trackedSeriesItemId = item.Id;
                sessionTrackCts = new CancellationTokenSource();
                _ = TrackSeriesPlaybackFromVlcAsync(currentSession, item.Id, sessionTrackCts.Token);
            }

            StatusText = $"Playing {item.Title}";
            launchSucceeded = true;
            _ = ReleasePlaybackGuardAsync();

            // Clean up the temporary playlist file after a delay
            _ = Task.Run(async () =>
            {
                await Task.Delay(10000);
                try
                {
                    if (File.Exists(playlistPath))
                    {
                        File.Delete(playlistPath);
                    }
                }
                catch
                {
                    // Ignore cleanup errors
                }
            });
        }
        catch (InvalidOperationException)
        {
            VlcPathStatus = "VLC could not be launched. Check the path in Settings.";
            StatusText = "Playback failed.";
        }
        finally
        {
            if (!launchSucceeded)
            {
                CanStartPlayback = true;
            }
        }
    }

    private string CreateXspfPlaylistWithStartTime(List<string> episodePaths, int startTimeSeconds)
    {
        var tempPath = Path.Combine(Path.GetTempPath(), $"vlc_resume_{Guid.NewGuid():N}.xspf");

        using var writer = new StreamWriter(tempPath, false, System.Text.Encoding.UTF8);
        writer.WriteLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
        writer.WriteLine("<playlist version=\"1\" xmlns=\"http://xspf.org/ns/0/\" xmlns:vlc=\"http://www.videolan.org/vlc/playlist/ns/0/\">");
        writer.WriteLine("  <title>Resume Playlist</title>");
        writer.WriteLine("  <trackList>");

        for (int i = 0; i < episodePaths.Count; i++)
        {
            var path = episodePaths[i];
            var fileUri = new Uri(path).AbsoluteUri;
            var fileName = Path.GetFileName(path);

            writer.WriteLine("    <track>");
            writer.WriteLine($"      <location>{System.Security.SecurityElement.Escape(fileUri)}</location>");
            writer.WriteLine($"      <title>{System.Security.SecurityElement.Escape(fileName)}</title>");
            writer.WriteLine($"      <extension application=\"http://www.videolan.org/vlc/playlist/0\">");
            writer.WriteLine($"        <vlc:id>{i}</vlc:id>");

            // Only add start-time option to the FIRST track
            if (i == 0)
            {
                writer.WriteLine($"        <vlc:option>start-time={startTimeSeconds}</vlc:option>");
            }

            writer.WriteLine($"      </extension>");
            writer.WriteLine("    </track>");
        }

        writer.WriteLine("  </trackList>");
        writer.WriteLine("</playlist>");

        return tempPath;
    }

    private bool EnsureVlcPathAvailable()
    {
        if (!string.IsNullOrWhiteSpace(VlcExecutablePath) && File.Exists(VlcExecutablePath))
        {
            vlcLocator.SetConfiguredPath(VlcExecutablePath);
            return true;
        }

        var discovered = vlcLocator.Discover();
        if (discovered is not null)
        {
            VlcExecutablePath = discovered;
            PersistVlcPath();
            return true;
        }

        VlcPathStatus = "VLC was not found. Use Settings to browse to vlc.exe.";
        StatusText = "Playback requires VLC.";
        MediaTabs.SelectedIndex = 2;
        return false;
    }

    private async Task ReleasePlaybackGuardAsync()
    {
        try
        {
            await Task.Delay(2000);
        }
        finally
        {
            CanStartPlayback = true;
        }
    }

    private void AttachPersistenceHandlers(IEnumerable<MediaItem> items)
    {
        foreach (var item in items)
        {
            item.PropertyChanged -= MediaItemOnPropertyChanged;
            item.PropertyChanged += MediaItemOnPropertyChanged;
        }
    }

    private void MediaItemOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is MediaItem && (e.PropertyName == nameof(MediaItem.Title) || e.PropertyName == nameof(MediaItem.ArtworkPath)))
        {
            PersistState();
        }
    }

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        return true;
    }

    private string ResolvePlayedPath(MediaItem item, string? explicitEpisodePath)
    {
        if (!string.IsNullOrWhiteSpace(explicitEpisodePath))
        {
            return explicitEpisodePath;
        }

        if (item.Kind == MediaKind.Series && item.PlaylistPaths.Count > 0)
        {
            return item.PlaylistPaths[0];
        }

        return item.PrimaryPath;
    }

    private async Task TrackSeriesPlaybackFromVlcAsync(VlcSession session, string seriesItemId, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(1200, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            if (!session.TryGetWindowTitle(out var title))
            {
                continue;
            }

            var matchedPath = MatchWindowTitleToEpisodePath(seriesItemId, title);
            if (string.IsNullOrWhiteSpace(matchedPath))
            {
                continue;
            }

            var alreadyCurrent =
                string.Equals(appState.Playback.LastItemId, seriesItemId, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(appState.Playback.LastFilePath, matchedPath, StringComparison.OrdinalIgnoreCase);

            if (alreadyCurrent)
            {
                continue;
            }

            await Dispatcher.InvokeAsync(() =>
            {
                if (!string.Equals(trackedSeriesItemId, seriesItemId, StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                var seriesItem = SeriesItems.FirstOrDefault(x => string.Equals(x.Id, seriesItemId, StringComparison.OrdinalIgnoreCase));
                if (seriesItem is null)
                {
                    return;
                }

                appState.Playback.LastItemId = seriesItemId;
                appState.Playback.LastFilePath = matchedPath;
                appState.Playback.LastStartedUtc = DateTime.UtcNow;
                appState.Playback.LastKnownTimeSeconds = null;
                UpdateRecentSeriesPlays(seriesItem, matchedPath);
                stateStore.Save(appState);
                if (SelectedSeriesItem is not null && string.Equals(SelectedSeriesItem.Id, seriesItemId, StringComparison.OrdinalIgnoreCase))
                {
                    RefreshSelectedSeriesEpisodeEntries();
                }
            });
        }
    }

    private string? MatchWindowTitleToEpisodePath(string seriesItemId, string windowTitle)
    {
        var seriesItem = SeriesItems.FirstOrDefault(x => string.Equals(x.Id, seriesItemId, StringComparison.OrdinalIgnoreCase));
        if (seriesItem is null || seriesItem.PlaylistPaths.Count == 0)
        {
            return null;
        }

        var normalizedTitle = NormalizeVlcWindowTitle(windowTitle);
        if (string.IsNullOrWhiteSpace(normalizedTitle))
        {
            return null;
        }

        var normalizedTitleToken = NormalizeMatchToken(normalizedTitle);

        foreach (var path in seriesItem.PlaylistPaths)
        {
            var leaf = Path.GetFileName(path);
            var leafNoExt = Path.GetFileNameWithoutExtension(path);
            if (string.Equals(normalizedTitle, leaf, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(normalizedTitle, leafNoExt, StringComparison.OrdinalIgnoreCase) ||
                normalizedTitle.Contains(leafNoExt, StringComparison.OrdinalIgnoreCase))
            {
                return path;
            }

            var normalizedLeafToken = NormalizeMatchToken(leafNoExt);
            if (normalizedLeafToken.Length >= 5 &&
                (normalizedTitleToken.Contains(normalizedLeafToken, StringComparison.Ordinal) ||
                 normalizedLeafToken.Contains(normalizedTitleToken, StringComparison.Ordinal)))
            {
                return path;
            }
        }

        return null;
    }

    private static string NormalizeVlcWindowTitle(string windowTitle)
    {
        var normalized = windowTitle.Trim();
        var markerIndex = normalized.IndexOf("VLC media player", StringComparison.OrdinalIgnoreCase);
        if (markerIndex < 0)
        {
            return normalized;
        }

        normalized = normalized[..markerIndex].Trim();
        normalized = normalized.TrimEnd('-', '–', '—', '|');
        return normalized.Trim();
    }

    private static string NormalizeMatchToken(string text)
    {
        var chars = text.Where(char.IsLetterOrDigit)
            .Select(char.ToLowerInvariant)
            .ToArray();
        return new string(chars);
    }

    private void UpdateRecentSeriesPlays(MediaItem item, string playedFilePath)
    {
        if (item.Kind != MediaKind.Series || string.IsNullOrWhiteSpace(item.Id) || string.IsNullOrWhiteSpace(playedFilePath))
        {
            return;
        }

        var snapshot = appState.Playback;
        snapshot.RecentSeriesPlays.RemoveAll(x =>
            string.Equals(x.SeriesItemId, item.Id, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(x.EpisodePath, playedFilePath, StringComparison.OrdinalIgnoreCase));

        snapshot.RecentSeriesPlays.Insert(0, new RecentSeriesPlay
        {
            SeriesItemId = item.Id,
            EpisodePath = playedFilePath,
            PlayedUtc = DateTime.UtcNow
        });

        var keepForSeries = snapshot.RecentSeriesPlays
            .Where(x => string.Equals(x.SeriesItemId, item.Id, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(x => x.PlayedUtc)
            .Take(3)
            .Select(x => x.EpisodePath)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        snapshot.RecentSeriesPlays.RemoveAll(x =>
            string.Equals(x.SeriesItemId, item.Id, StringComparison.OrdinalIgnoreCase) &&
            !keepForSeries.Contains(x.EpisodePath));
    }

    private void RefreshSelectedSeriesEpisodeEntries()
    {
        if (SelectedSeriesItem is null)
        {
            SelectedSeriesEpisodeEntries = [];
            return;
        }

        var markerLookup = BuildEpisodeMarkerRankLookup(SelectedSeriesItem.Id);
        SelectedSeriesEpisodeEntries = new ObservableCollection<EpisodeDisplayEntry>(
            SelectedSeriesItem.PlaylistPaths.Select(path =>
            {
                markerLookup.TryGetValue(path, out var rank);
                return new EpisodeDisplayEntry(path, rank);
            }));
    }

    private Dictionary<string, int> BuildEpisodeMarkerRankLookup(string seriesItemId)
    {
        if (string.IsNullOrWhiteSpace(seriesItemId))
        {
            return new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        }

        return appState.Playback.RecentSeriesPlays
            .Where(x => string.Equals(x.SeriesItemId, seriesItemId, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(x => x.PlayedUtc)
            .Take(3)
            .Select((x, index) => new { x.EpisodePath, Rank = index + 1 })
            .ToDictionary(x => x.EpisodePath, x => x.Rank, StringComparer.OrdinalIgnoreCase);
    }

    public sealed class EpisodeDisplayEntry
    {
        public EpisodeDisplayEntry(string filePath, int? markerRank)
        {
            FilePath = filePath;
            MarkerRank = markerRank;
            MarkerVisibility = markerRank.HasValue ? Visibility.Visible : Visibility.Collapsed;
            MarkerOpacity = markerRank switch
            {
                1 => 1.0,
                2 => 0.65,
                3 => 0.35,
                _ => 1.0
            };
        }

        public string FilePath { get; }
        public int? MarkerRank { get; }
        public Visibility MarkerVisibility { get; }
        public double MarkerOpacity { get; }
    }
}
