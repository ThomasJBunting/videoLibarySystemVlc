namespace VideoLibrarySystemVlc.Themes;

public enum AppThemeId
{
	Light,
	Dark,
	Retro
}

public sealed class ThemeDefinition
{
	public required AppThemeId Id { get; init; }
	public required string Name { get; init; }
	public required string IconGlyph { get; init; }
	public required Uri ResourceUri { get; init; }
	public bool IsDark { get; init; }
	public string? ImagePath { get; init; }
}
