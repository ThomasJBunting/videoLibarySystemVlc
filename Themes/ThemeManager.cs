using System.Windows;
using System.Windows.Controls;
using Button = System.Windows.Controls.Button;
using Application = System.Windows.Application;
using VideoLibrarySystemVlc.Models;

namespace VideoLibrarySystemVlc.Themes;

public static class ThemeManager
{
	private static readonly List<ThemeDefinition> Themes =
	[
		new ThemeDefinition
		{
			Id = AppThemeId.Light,
			Name = "Light",
			IconGlyph = "☀",
			ResourceUri = new Uri("/ResourceDictionaries/LightTheme.xaml", UriKind.Relative),
			IsDark = false
		},
		new ThemeDefinition
		{
			Id = AppThemeId.Dark,
			Name = "Dark",
			IconGlyph = "☾",
			ResourceUri = new Uri("/ResourceDictionaries/DarkTheme.xaml", UriKind.Relative),
			IsDark = true
		},
		new ThemeDefinition
		{
			Id = AppThemeId.Retro,
			Name = "Retro 90s",
			IconGlyph = "◉",
			ResourceUri = new Uri("/ResourceDictionaries/RetroTheme.xaml", UriKind.Relative),
			IsDark = true
		}
	];

	public static IReadOnlyList<ThemeDefinition> AvailableThemes => Themes;

	public static ThemeDefinition GetTheme(AppThemeId id) =>
		Themes.FirstOrDefault(theme => theme.Id == id) ?? Themes[0];

	public static ThemeDefinition GetThemeByName(string themeName)
	{
		if (string.IsNullOrEmpty(themeName))
		{
			return Themes[0];
		}

		return Themes.FirstOrDefault(theme => theme.Id.ToString() == themeName) ?? Themes[0];
	}

	public static AppThemeId GetNextTheme(AppThemeId currentTheme)
	{
		var currentIndex = Themes.FindIndex(theme => theme.Id == currentTheme);
		if (currentIndex < 0)
		{
			return AppThemeId.Light;
		}

		var nextIndex = (currentIndex + 1) % Themes.Count;
		return Themes[nextIndex].Id;
	}

	public static AppThemeId GetToggleTheme(ThemeState themeState)
	{
		var currentId = GetThemeByName(themeState.CurrentThemeId).Id;
		var previousId = string.IsNullOrEmpty(themeState.PreviousThemeId) 
			? (currentId == AppThemeId.Light ? AppThemeId.Dark : AppThemeId.Light)
			: GetThemeByName(themeState.PreviousThemeId).Id;

		return previousId;
	}

	public static void Apply(AppThemeId id)
	{
		var theme = GetTheme(id);
		var appResources = Application.Current.Resources.MergedDictionaries;

		appResources.Clear();
		appResources.Add(new ResourceDictionary { Source = theme.ResourceUri });
	}

	public static void ApplyAndUpdateState(AppThemeId id, ThemeState themeState)
	{
		var newThemeName = id.ToString();
		themeState.PreviousThemeId = themeState.CurrentThemeId;
		themeState.CurrentThemeId = newThemeName;
		Apply(id);
	}

	public static ThemeDefinition GetCurrentTheme()
	{
		if (Application.Current is null)
		{
			return Themes[0];
		}

		foreach (var dictionary in Application.Current.Resources.MergedDictionaries)
		{
			if (dictionary.Source is null)
			{
				continue;
			}

			var result = Themes.FirstOrDefault(theme => dictionary.Source.OriginalString.Contains(theme.ResourceUri.OriginalString, StringComparison.OrdinalIgnoreCase));
			if (result is not null)
			{
				return result;
			}
		}

		return Themes[0];
	}

	public static void ConfigureButtonTheme(Button button, ThemeDefinition theme)
	{
		button.Content = theme.IconGlyph;
		button.ToolTip = $"Switch to {theme.Name} mode";
	}
}

