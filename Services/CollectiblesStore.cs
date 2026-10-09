using System.Text.Json;
using System.Text.Json.Serialization;
using VideoLibrarySystemVlc.Models;

namespace VideoLibrarySystemVlc.Services;

/// <summary>
/// Manages persistence of Back Rooms state (gems, collectibles, themes, settings).
/// Separate from the main AppState to avoid bloating the primary JSON.
/// </summary>
public sealed class CollectiblesStore
{
	private static readonly JsonSerializerOptions Options = new()
	{
		WriteIndented = true
	};

	/// <summary>
	/// Load the back rooms state or create a new one if it doesn't exist.
	/// </summary>
	public BackRoomsState LoadOrCreate()
	{
		Directory.CreateDirectory(AppPaths.BaseFolder);

		if (!File.Exists(AppPaths.BackRoomsStateFile))
		{
			return new BackRoomsState();
		}

		try
		{
			var json = File.ReadAllText(AppPaths.BackRoomsStateFile);
			var state = JsonSerializer.Deserialize<BackRoomsState>(json, Options) ?? new BackRoomsState();
			return state;
		}
		catch
		{
			BackupCorruptState();
			return new BackRoomsState();
		}
	}

	/// <summary>
	/// Save the back rooms state to disk.
	/// </summary>
	public void Save(BackRoomsState state)
	{
		Directory.CreateDirectory(AppPaths.BaseFolder);
		var json = JsonSerializer.Serialize(state, Options);
		File.WriteAllText(AppPaths.BackRoomsStateFile, json);
	}

	/// <summary>
	/// Add a newly won collectible to the state and save.
	/// </summary>
	public void AddCollectible(BackRoomsState state, Collectible collectible)
	{
		state.CollectedItems.Add(collectible);
		Save(state);
	}

	/// <summary>
	/// Add a newly collected theme to the state and save.
	/// </summary>
	public void AddTheme(BackRoomsState state, CollectedTheme theme)
	{
		state.CollectedThemes.Add(theme);
		Save(state);
	}

	/// <summary>
	/// JSON wrapper for deserializing collectibles.json with both collectibles and themes arrays.
	/// Supports backward compatibility: if the root is an array, treat it as collectibles-only format.
	/// </summary>
	private sealed class CollectiblesJsonWrapper
	{
		[JsonPropertyName("collectibles")]
		public List<CollectibleDefinition>? Collectibles { get; set; }

		[JsonPropertyName("themes")]
		public List<ThemeRewardDefinition>? Themes { get; set; }
	}

	/// <summary>
	/// Parse JSON content supporting both new format (object with collectibles/themes arrays)
	/// and old format (array of collectibles).
	/// Returns a tuple of (collectibles, themes).
	/// </summary>
	public static (List<CollectibleDefinition>, List<ThemeRewardDefinition>) ParseCollectiblesJson(string json)
	{
		try
		{
			var trimmedJson = json.Trim();

			// Try new format first (object with collectibles/themes)
			if (trimmedJson.StartsWith("{"))
			{
				try
				{
					var wrapper = JsonSerializer.Deserialize<CollectiblesJsonWrapper>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
					var collectibles = wrapper?.Collectibles ?? [];
					var themes = wrapper?.Themes ?? [];
					return (collectibles, themes);
				}
				catch
				{
					// Fall through to try old format
				}
			}

			// Try old format (array of collectibles only)
			if (trimmedJson.StartsWith("["))
			{
				try
				{
					var collectibles = JsonSerializer.Deserialize<List<CollectibleDefinition>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [];
					return (collectibles, []);
				}
				catch
				{
					// Fall through to error handling
				}
			}

			System.Diagnostics.Debug.WriteLine("[CollectiblesStore] Unrecognized JSON format");
			return ([], []);
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"[CollectiblesStore] Error parsing collectibles JSON: {ex.Message}");
			return ([], []);
		}
	}

	private static void BackupCorruptState()
	{
		if (!File.Exists(AppPaths.BackRoomsStateFile))
		{
			return;
		}

		var corruptPath = AppPaths.BackRoomsStateFile + ".corrupt";
		File.Delete(corruptPath);
		File.Move(AppPaths.BackRoomsStateFile, corruptPath);
	}
}
