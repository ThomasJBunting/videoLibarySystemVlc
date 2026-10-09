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

/// <summary>
/// Defines a theme reward available in loot crates (from remote JSON).
/// Similar to CollectibleDefinition but for themes with additional configuration.
/// </summary>
public sealed class ThemeRewardDefinition
{
	/// <summary>
	/// Unique identifier for this theme (should match AppThemeId enum or be a new custom theme).
	/// </summary>
	public string Id { get; set; } = string.Empty;

	/// <summary>
	/// Display name of the theme.
	/// </summary>
	public string Name { get; set; } = string.Empty;

	/// <summary>
	/// Description text shown when viewing the theme.
	/// </summary>
	public string Description { get; set; } = string.Empty;

	/// <summary>
	/// Icon glyph for the theme (single character or emoji).
	/// </summary>
	public string IconGlyph { get; set; } = "◉";

	/// <summary>
	/// Drop weight for probability calculation (higher = more common).
	/// </summary>
	public int DropWeight { get; set; } = 1;

	/// <summary>
	/// Optional rarity tier label (e.g., "Common", "Rare", "Epic").
	/// </summary>
	public string? Rarity { get; set; }

	/// <summary>
	/// Whether this theme has a dark background (true) or light background (false).
	/// </summary>
	public bool IsDark { get; set; } = true;

	/// <summary>
	/// URI to the theme's resource dictionary file (e.g., "/ResourceDictionaries/RetroTheme.xaml").
	/// </summary>
	public string ResourceUri { get; set; } = string.Empty;

	/// <summary>
	/// Optional URL to a preview/background image (supports http://, https://, or file://).
	/// </summary>
	public string? ImageUrl { get; set; }

	/// <summary>
	/// Optional list of additional image URLs related to this theme (e.g., wood panel effects, backgrounds).
	/// Can be used for theme decoration or customization.
	/// </summary>
	public List<string>? RelatedImageUrls { get; set; }

	/// <summary>
	/// Optional theme metadata/configuration (colors, settings, etc.) as JSON object.
	/// Custom implementations can deserialize this as needed.
	/// </summary>
	public Dictionary<string, object>? ThemeData { get; set; }
}
