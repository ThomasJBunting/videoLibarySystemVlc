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
}
