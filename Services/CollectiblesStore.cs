using System.Text.Json;
using VideoLibrarySystemVlc.Models;

namespace VideoLibrarySystemVlc.Services;

/// <summary>
/// Manages persistence of Back Rooms state (gems, collectibles, settings).
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
