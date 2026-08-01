namespace VideoLibrarySystemVlc.Models;

/// <summary>
/// Defines a collectible item available in loot crates (from remote JSON).
/// </summary>
public sealed class CollectibleDefinition
{
	/// <summary>
	/// Unique identifier for this collectible type.
	/// </summary>
	public string Id { get; set; } = string.Empty;

	/// <summary>
	/// Display name of the collectible.
	/// </summary>
	public string Name { get; set; } = string.Empty;

	/// <summary>
	/// Description text shown when viewing the collectible.
	/// </summary>
	public string Description { get; set; } = string.Empty;

	/// <summary>
	/// URL to the image (supports http://, https://, or file://).
	/// </summary>
	public string ImageUrl { get; set; } = string.Empty;

	/// <summary>
	/// Drop weight for probability calculation (higher = more common).
	/// </summary>
	public int DropWeight { get; set; } = 1;

	/// <summary>
	/// Optional rarity tier label (e.g., "Common", "Rare", "Epic").
	/// </summary>
	public string? Rarity { get; set; }
}
