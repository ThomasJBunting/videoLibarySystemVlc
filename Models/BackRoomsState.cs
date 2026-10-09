namespace VideoLibrarySystemVlc.Models;

/// <summary>
/// Persistent state for the Back Rooms (video rental desk) feature.
/// Stored separately from the main AppState.
/// </summary>
public sealed class BackRoomsState
{
	/// <summary>
	/// Current gem count (editable by user).
	/// </summary>
	public int Gems { get; set; } = 0;

	/// <summary>
	/// Last time a gem was automatically awarded (UTC).
	/// </summary>
	public DateTime? LastGemAwardUtc { get; set; }

	/// <summary>
	/// List of collectibles the user has won.
	/// </summary>
	public List<Collectible> CollectedItems { get; set; } = [];

	/// <summary>
	/// List of themes the user has collected (unlocked) from loot crates.
	/// Does not include default themes (Light, Dark).
	/// </summary>
	public List<CollectedTheme> CollectedThemes { get; set; } = [];

	/// <summary>
	/// URL or file path to the collectibles definitions JSON.
	/// </summary>
	public string? CollectiblesSourceUrl { get; set; }

	/// <summary>
	/// URL or file path to the ticker tape reviews JSON.
	/// </summary>
	public string? TickerReviewsUrl { get; set; }

	/// <summary>
	/// Whether ticker tape is enabled.
	/// </summary>
	public bool TickerTapeEnabled { get; set; } = true;

	/// <summary>
	/// URL to redirect to when "Pay Your Late Fee" is clicked.
	/// </summary>
	public string? LateFeeUrl { get; set; } = "https://www.google.com";

	/// <summary>
	/// Theme selection and history state.
	/// </summary>
	public ThemeState ThemeState { get; set; } = new();
}

/// <summary>
/// Represents a theme that the user has collected/unlocked from a loot crate.
/// </summary>
public sealed class CollectedTheme
{
	/// <summary>
	/// Unique identifier matching the ThemeRewardDefinition.
	/// </summary>
	public string Id { get; set; } = string.Empty;

	/// <summary>
	/// Display name of the theme.
	/// </summary>
	public string Name { get; set; } = string.Empty;

	/// <summary>
	/// Description text.
	/// </summary>
	public string Description { get; set; } = string.Empty;

	/// <summary>
	/// When this theme was collected (UTC).
	/// </summary>
	public DateTime CollectedDateUtc { get; set; }

	/// <summary>
	/// Optional rarity tier label.
	/// </summary>
	public string? Rarity { get; set; }

	/// <summary>
	/// Optional theme metadata/configuration copied from the definition.
	/// </summary>
	public Dictionary<string, object>? ThemeData { get; set; }
}
