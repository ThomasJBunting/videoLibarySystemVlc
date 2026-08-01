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

	// Back Rooms services and state
	private readonly CollectiblesStore collectiblesStore = new();
	private readonly BackRoomsService backRoomsService;
	private readonly TickerTapeService tickerTapeService = new();
	private BackRoomsState backRoomsState;
	private System.Windows.Threading.DispatcherTimer? gemTimer;
	private System.Windows.Media.Animation.Storyboard? tickerStoryboard;
	private List<CollectibleDefinition> availableCollectibles = [];

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

	// Back Rooms properties
	private ObservableCollection<Collectible> backRoomsCollectibles = [];
	private Collectible? selectedCollectible;
	private int backRoomsGems = 0;
	private string tickerTapeText = string.Empty;
	private bool tickerTapeVisible = true;
	public MainWindow()
	{
		InitializeComponent();
		Icon = IconFactory.CreateWindowIcon();

		appState = stateStore.LoadOrCreate();
		vlcLocator = new VlcLocator(appState.Settings.VlcExecutablePath);
		vlcLauncher = new VlcLauncher(vlcLocator);
		trayIconImage = IconFactory.CreateTrayIcon();

		// Initialize Back Rooms
		backRoomsState = collectiblesStore.LoadOrCreate();
		backRoomsService = new BackRoomsService(collectiblesStore);

		DataContext = this;
		LoadStateToUi();
		AutoConfigureVlcPath();
		InitializeBackRooms();

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

    // Back Rooms properties
    public ObservableCollection<Collectible> BackRoomsCollectibles
    {
        get => backRoomsCollectibles;
        set => SetField(ref backRoomsCollectibles, value);
    }

    public Collectible? SelectedCollectible
    {
        get => selectedCollectible;
        set => SetField(ref selectedCollectible, value);
    }

    public int BackRoomsGems
    {
        get => backRoomsGems;
        set
        {
            if (SetField(ref backRoomsGems, value))
            {
                // Save to state when gems change
                backRoomsState.Gems = value;
                collectiblesStore.Save(backRoomsState);
            }
        }
    }

    public string TickerTapeText
    {
        get => tickerTapeText;
        set
        {
            if (SetField(ref tickerTapeText, value))
            {
                // Restart animation when text changes
                Dispatcher.BeginInvoke(new Action(StartTickerAnimation), System.Windows.Threading.DispatcherPriority.Loaded);
            }
        }
    }

    public bool TickerTapeVisible
    {
        get => tickerTapeVisible;
        set
        {
            if (SetField(ref tickerTapeVisible, value))
            {
                // Start/stop animation when visibility changes
                if (value)
                {
                    Dispatcher.BeginInvoke(new Action(StartTickerAnimation), System.Windows.Threading.DispatcherPriority.Loaded);
                }
                else
                {
                    StopTickerAnimation();
                }
            }
        }
    }

    public string? CollectiblesSourceUrl
    {
        get => appState.Settings.CollectiblesSourceUrl;
        set
        {
            if (appState.Settings.CollectiblesSourceUrl != value)
            {
                appState.Settings.CollectiblesSourceUrl = value;
                OnPropertyChanged();
            }
        }
    }

    public string? TickerReviewsUrl
    {
        get => appState.Settings.TickerReviewsUrl;
        set
        {
            if (appState.Settings.TickerReviewsUrl != value)
            {
                appState.Settings.TickerReviewsUrl = value;
                OnPropertyChanged();
            }
        }
    }

    public string? LateFeeUrl
    {
        get => appState.Settings.LateFeeUrl;
        set
        {
            if (appState.Settings.LateFeeUrl != value)
            {
                appState.Settings.LateFeeUrl = value;
                OnPropertyChanged();
            }
        }
    }

    public AppSettings Settings => appState.Settings;

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

    private void InitializeBackRooms()
    {
        // Load state
        BackRoomsGems = backRoomsState.Gems;
        BackRoomsCollectibles = new ObservableCollection<Collectible>(backRoomsState.CollectedItems);
        TickerTapeVisible = appState.Settings.TickerTapeEnabled;

        // Set up gem timer (check every minute)
        gemTimer = new System.Windows.Threading.DispatcherTimer
        {
            Interval = TimeSpan.FromMinutes(1)
        };
        gemTimer.Tick += GemTimer_Tick;
        gemTimer.Start();

        // Check for gem award immediately
        CheckAndAwardGem();

        // Set up ticker tape animation
        StartTickerAnimation();

        // Load collectibles and reviews asynchronously
        _ = LoadBackRoomsDataAsync();
    }

    private void GemTimer_Tick(object? sender, EventArgs e)
    {
        CheckAndAwardGem();
    }

    private void CheckAndAwardGem()
    {
        if (backRoomsService.TryAwardDailyGem(backRoomsState))
        {
            BackRoomsGems = backRoomsState.Gems;
            StatusText = "You earned a gem! 💎";
        }

        // Update next gem timer display
        var timeUntilNext = backRoomsService.GetTimeUntilNextGem(backRoomsState);
        if (NextGemTimerText != null)
        {
            NextGemTimerText.Text = $"Next gem in: {timeUntilNext.Hours:D2}:{timeUntilNext.Minutes:D2}:{timeUntilNext.Seconds:D2}";
        }
    }

    private void StartTickerAnimation()
    {
        if (TickerTextBlock == null || TickerCanvas == null || !TickerTapeVisible)
        {
            return;
        }

        // Stop any existing animation
        StopTickerAnimation();

        // Measure the text to calculate animation distance
        TickerTextBlock.Measure(new System.Windows.Size(double.PositiveInfinity, double.PositiveInfinity));
        var textWidth = TickerTextBlock.DesiredSize.Width;
        var canvasWidth = TickerCanvas.ActualWidth;

        if (canvasWidth <= 0 || textWidth <= 0)
        {
            return;
        }

        // Get the storyboard from resources
        tickerStoryboard = TickerTextBlock.Resources["TickerAnimation"] as System.Windows.Media.Animation.Storyboard;
        if (tickerStoryboard == null)
        {
            return;
        }

        // Configure animation: start from right edge, scroll past left edge
        var animation = tickerStoryboard.Children[0] as System.Windows.Media.Animation.DoubleAnimation;
        if (animation != null)
        {
            animation.From = canvasWidth;
            animation.To = -textWidth;

            // Calculate duration: ~60 pixels per second for smooth scrolling
            var totalDistance = canvasWidth + textWidth;
            var duration = TimeSpan.FromSeconds(totalDistance / 60.0);
            animation.Duration = new System.Windows.Duration(duration);
        }

        // Start the animation
        tickerStoryboard.Begin();
    }

    private void StopTickerAnimation()
    {
        tickerStoryboard?.Stop();
    }

    private async Task LoadBackRoomsDataAsync()
    {
        try
        {
            // Load collectibles definitions
            var collectiblesUrl = appState.Settings.CollectiblesSourceUrl;
            if (!string.IsNullOrEmpty(collectiblesUrl))
            {
                availableCollectibles = await backRoomsService.LoadCollectibleDefinitionsAsync(collectiblesUrl);
                if (AvailableCollectiblesText != null)
                {
                    AvailableCollectiblesText.Text = availableCollectibles.Count > 0
                        ? $"{availableCollectibles.Count} collectibles available to win!"
                        : "No collectibles configured. Set the URL in settings.";
                }
            }

            // Load ticker tape reviews
            var tickerUrl = appState.Settings.TickerReviewsUrl;
            if (!string.IsNullOrEmpty(tickerUrl))
            {
                var reviews = await tickerTapeService.LoadReviewsAsync(tickerUrl);
                TickerTapeText = tickerTapeService.FormatReviewsForDisplay(reviews);
            }
            else
            {
                TickerTapeText = "Welcome to the Video Rental Desk! Set up ticker tape reviews in settings.";
            }
        }
        catch (Exception ex)
        {
            StatusText = $"Error loading Back Rooms data: {ex.Message}";
        }
    }

    private async void OpenLootCrate_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // Debug: Show current state
            StatusText = $"Gems: {BackRoomsGems}, Collectibles loaded: {availableCollectibles.Count}";
            System.Diagnostics.Debug.WriteLine($"[MainWindow] Opening loot crate. Gems: {BackRoomsGems}, Collectibles: {availableCollectibles.Count}");

            if (BackRoomsGems < 1)
            {
                System.Windows.MessageBox.Show("You need at least 1 gem to open a loot crate!", "Not Enough Gems", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (availableCollectibles.Count == 0)
            {
                var msg = "No collectibles are configured. Check your settings.\n\n";
                msg += $"Collectibles URL: {appState.Settings.CollectiblesSourceUrl ?? "(not set)"}\n";
                msg += "Make sure you've saved the URL in Settings and it points to a valid JSON file.";
                System.Windows.MessageBox.Show(msg, "No Collectibles", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            StatusText = "Opening loot crate...";
            var wonCollectible = await backRoomsService.OpenLootCrateAsync(backRoomsState, availableCollectibles);

            if (wonCollectible != null)
            {
                BackRoomsGems = backRoomsState.Gems;
                BackRoomsCollectibles.Add(wonCollectible);
                SelectedCollectible = wonCollectible;

                System.Windows.MessageBox.Show($"Congratulations! You won:\n\n{wonCollectible.Name}\n\n{wonCollectible.Description}", 
                    "Loot Crate Opened!", MessageBoxButton.OK, MessageBoxImage.Information);

                StatusText = $"Opened loot crate and won: {wonCollectible.Name}";
                System.Diagnostics.Debug.WriteLine($"[MainWindow] Successfully won: {wonCollectible.Name}");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("[MainWindow] OpenLootCrateAsync returned null");
                System.Windows.MessageBox.Show("Failed to open loot crate. Check the Output window for details.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                StatusText = "Failed to open loot crate.";
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[MainWindow] Exception in OpenLootCrate_Click: {ex.Message}\n{ex.StackTrace}");
            System.Windows.MessageBox.Show($"Error opening loot crate: {ex.Message}\n\nCheck the Output window for details.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            StatusText = $"Error: {ex.Message}";
        }
    }

    private void PayLateFee_Click(object sender, RoutedEventArgs e)
    {
        var url = appState.Settings.LateFeeUrl ?? "https://www.google.com";
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
            StatusText = "Redirected to pay late fee...";
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Failed to open URL: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BackRoomsSectionList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (BackRoomsSectionList == null || CollectiblesSection == null || LootCrateInfoSection == null)
        {
            return;
        }

        var selectedIndex = BackRoomsSectionList.SelectedIndex;

        CollectiblesSection.Visibility = selectedIndex == 0 ? Visibility.Visible : Visibility.Collapsed;
        LootCrateInfoSection.Visibility = selectedIndex == 1 ? Visibility.Visible : Visibility.Collapsed;
    }

    private void GemCountTextBox_LostFocus(object sender, RoutedEventArgs e)
    {
        // Parse gem count from text box (supports manual editing)
        if (int.TryParse(GemCountTextBox.Text, out var gemCount))
        {
            BackRoomsGems = Math.Max(0, gemCount);
        }
        else
        {
            // Reset to current value if invalid
            GemCountTextBox.Text = BackRoomsGems.ToString();
        }
    }

    private async void ReloadBackRoomsData_Click(object sender, RoutedEventArgs e)
    {
        StatusText = "Reloading Back Rooms data...";
        await LoadBackRoomsDataAsync();
        StatusText = $"Reloaded! {availableCollectibles.Count} collectibles available.";
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
            gemTimer?.Stop();
            StopTickerAnimation();
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

    private void TickerTapeToggle_Click(object sender, RoutedEventArgs e)
    {
        appState.Settings.TickerTapeEnabled = TickerTapeVisible;
        stateStore.Save(appState);
        StatusText = TickerTapeVisible ? "Ticker tape enabled." : "Ticker tape disabled.";
    }

    private async void SaveCollectiblesUrl_Click(object sender, RoutedEventArgs e)
    {
        stateStore.Save(appState);
        StatusText = "Collectibles URL saved.";

        // Reload collectibles
        if (!string.IsNullOrEmpty(CollectiblesSourceUrl))
        {
            availableCollectibles = await backRoomsService.LoadCollectibleDefinitionsAsync(CollectiblesSourceUrl);
            if (AvailableCollectiblesText != null)
            {
                AvailableCollectiblesText.Text = availableCollectibles.Count > 0
                    ? $"{availableCollectibles.Count} collectibles available to win!"
                    : "No collectibles found at URL.";
            }
            StatusText = $"Loaded {availableCollectibles.Count} collectibles.";
        }
    }

    private async void SaveTickerReviewsUrl_Click(object sender, RoutedEventArgs e)
    {
        stateStore.Save(appState);
        StatusText = "Ticker reviews URL saved.";

        // Reload reviews
        if (!string.IsNullOrEmpty(TickerReviewsUrl))
        {
            var reviews = await tickerTapeService.LoadReviewsAsync(TickerReviewsUrl);
            TickerTapeText = tickerTapeService.FormatReviewsForDisplay(reviews);

            StatusText = $"Loaded {reviews.Count} reviews.";
        }
    }

    private void SaveLateFeeUrl_Click(object sender, RoutedEventArgs e)
    {
        stateStore.Save(appState);
        StatusText = "Late fee URL saved.";
    }

    private void CollectibleEffectsToggle_Click(object sender, RoutedEventArgs e)
    {
        stateStore.Save(appState);
        StatusText = appState.Settings.CollectibleCardEffectsEnabled 
            ? "Collectible card 3D tilt effects enabled." 
            : "Collectible card 3D tilt effects disabled.";
    }

    private void CollectibleShimmerToggle_Click(object sender, RoutedEventArgs e)
    {
        stateStore.Save(appState);
        StatusText = appState.Settings.CollectibleCardShimmerEnabled 
            ? "Collectible card water shimmer effects enabled." 
            : "Collectible card water shimmer effects disabled.";
    }

    private void VideoEffectsToggle_Click(object sender, RoutedEventArgs e)
    {
        stateStore.Save(appState);
        StatusText = appState.Settings.VideoCardEffectsEnabled 
            ? "Video card 3D tilt effects enabled." 
            : "Video card 3D tilt effects disabled.";
    }

    private void VideoShimmerToggle_Click(object sender, RoutedEventArgs e)
    {
        stateStore.Save(appState);
        StatusText = appState.Settings.VideoCardShimmerEnabled 
            ? "Video card water shimmer effects enabled." 
            : "Video card water shimmer effects disabled.";
    }

    private void CollectibleHoloFoilToggle_Click(object sender, RoutedEventArgs e)
    {
        stateStore.Save(appState);
        StatusText = appState.Settings.CollectibleCardHoloFoilEnabled 
            ? "Collectible card holographic foil edge effects enabled." 
            : "Collectible card holographic foil edge effects disabled.";
    }

    private void VideoHoloFoilToggle_Click(object sender, RoutedEventArgs e)
    {
        stateStore.Save(appState);
        StatusText = appState.Settings.VideoCardHoloFoilEnabled 
            ? "Video card holographic foil edge effects enabled." 
            : "Video card holographic foil edge effects disabled.";
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
            if (currentSession is not null)
            {
                sessionTrackCts = new CancellationTokenSource();
                if (item.Kind == MediaKind.Series)
                {
                    trackedSeriesItemId = item.Id;
                    _ = TrackSeriesPlaybackFromVlcAsync(currentSession, item.Id, sessionTrackCts.Token);
                }
                else
                {
                    _ = TrackMoviePlaybackFromVlcAsync(currentSession, item.Id, sessionTrackCts.Token);
                }
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
            string.Equals(appState.Playback.LastFilePath, resumePlaylist[0], StringComparison.OrdinalIgnoreCase))
        {
            // Use the actual tracked position if available, otherwise estimate
            if (appState.Playback.LastKnownTimeSeconds.HasValue && appState.Playback.LastKnownTimeSeconds.Value > 0)
            {
                // Use real tracked position - 60 seconds
                timeOffsetSeconds = Math.Max(0, appState.Playback.LastKnownTimeSeconds.Value - 60);
            }
            else if (appState.Playback.LastStartedUtc.HasValue)
            {
                // Fallback: Estimate the current position based on elapsed time
                var elapsed = DateTime.UtcNow - appState.Playback.LastStartedUtc.Value;
                var estimatedCurrentSeconds = (int)elapsed.TotalSeconds;

                // Start 60 seconds before the estimated position, but not before 0
                timeOffsetSeconds = Math.Max(0, estimatedCurrentSeconds - 60);
            }
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

            // Use a random port for RC interface
            var rcPort = Random.Shared.Next(50000, 50100);

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
            startInfo.ArgumentList.Add($"--rc-host=localhost:{rcPort}");
            startInfo.ArgumentList.Add(playlistPath);

            var process = Process.Start(startInfo) ?? throw new InvalidOperationException("Unable to start VLC.");
            currentSession = new VlcSession(process, rcPort);

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

    private void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
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
        VlcRcClient? rcClient = null;
        var rcConnectionAttempted = false;
        var lastPositionPollTicks = 0L;
        const long pollIntervalTicks = 10 * TimeSpan.TicksPerSecond; // 30 seconds

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

            // Try to connect to RC interface once
            if (!rcConnectionAttempted)
            {
                rcConnectionAttempted = true;
                rcClient = await session.GetRcClientAsync(cancellationToken);
            }

            // Poll VLC for current playback position every 30 seconds
            if (rcClient is not null && DateTime.UtcNow.Ticks - lastPositionPollTicks >= pollIntervalTicks)
            {
                lastPositionPollTicks = DateTime.UtcNow.Ticks;
                var currentSeconds = await rcClient.GetCurrentTimeSecondsAsync(cancellationToken);

                if (currentSeconds.HasValue && currentSeconds.Value > 0)
                {
                    await Dispatcher.InvokeAsync(() =>
                    {
                        if (string.Equals(appState.Playback.LastItemId, seriesItemId, StringComparison.OrdinalIgnoreCase))
                        {
                            appState.Playback.LastKnownTimeSeconds = currentSeconds.Value;
                            stateStore.Save(appState);
                        }
                    });
                }
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

    private async Task TrackMoviePlaybackFromVlcAsync(VlcSession session, string itemId, CancellationToken cancellationToken)
    {
        VlcRcClient? rcClient = null;
        var rcConnectionAttempted = false;
        var lastPositionPollTicks = 0L;
        const long pollIntervalTicks = 30 * TimeSpan.TicksPerSecond; // 30 seconds

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

            // Try to connect to RC interface once
            if (!rcConnectionAttempted)
            {
                rcConnectionAttempted = true;
                rcClient = await session.GetRcClientAsync(cancellationToken);
            }

            // Poll VLC for current playback position every 30 seconds
            if (rcClient is not null && DateTime.UtcNow.Ticks - lastPositionPollTicks >= pollIntervalTicks)
            {
                lastPositionPollTicks = DateTime.UtcNow.Ticks;
                var currentSeconds = await rcClient.GetCurrentTimeSecondsAsync(cancellationToken);

                if (currentSeconds.HasValue && currentSeconds.Value > 0)
                {
                    await Dispatcher.InvokeAsync(() =>
                    {
                        if (string.Equals(appState.Playback.LastItemId, itemId, StringComparison.OrdinalIgnoreCase))
                        {
                            appState.Playback.LastKnownTimeSeconds = currentSeconds.Value;
                            stateStore.Save(appState);
                        }
                    });
                }
            }
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
