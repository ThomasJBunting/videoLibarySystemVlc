namespace VideoLibrarySystemVlc.Models;

/// <summary>
/// Persistent state for theme selection and history.
/// </summary>
public sealed class ThemeState
{
	/// <summary>
	/// The currently active theme ID.
	/// </summary>
	public string CurrentThemeId { get; set; } = "Light";

	/// <summary>
	/// The previously used theme ID (for toggle functionality).
	/// </summary>
	public string? PreviousThemeId { get; set; }
}
