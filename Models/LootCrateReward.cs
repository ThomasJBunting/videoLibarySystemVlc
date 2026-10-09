namespace VideoLibrarySystemVlc.Models;

/// <summary>
/// Represents a reward from opening a loot crate: either a collectible or a theme.
/// </summary>
public sealed class LootCrateReward
{
	/// <summary>
	/// If true, this reward is a theme. If false, it's a collectible.
	/// </summary>
	public bool IsTheme { get; set; }

	/// <summary>
	/// The collectible reward (null if this is a theme reward).
	/// </summary>
	public Collectible? Collectible { get; set; }

	/// <summary>
	/// The theme reward (null if this is a collectible reward).
	/// </summary>
	public CollectedTheme? Theme { get; set; }

	/// <summary>
	/// Create a collectible reward.
	/// </summary>
	public static LootCrateReward FromCollectible(Collectible collectible) =>
		new() { IsTheme = false, Collectible = collectible, Theme = null };

	/// <summary>
	/// Create a theme reward.
	/// </summary>
	public static LootCrateReward FromTheme(CollectedTheme theme) =>
		new() { IsTheme = true, Collectible = null, Theme = theme };
}
