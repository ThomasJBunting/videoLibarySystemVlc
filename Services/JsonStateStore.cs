using System.Text.Json;
using System.Text.Json.Nodes;
using VideoLibrarySystemVlc.Models;

namespace VideoLibrarySystemVlc.Services;

public sealed class JsonStateStore
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true
    };

    public AppState LoadOrCreate()
    {
        Directory.CreateDirectory(AppPaths.BaseFolder);

        if (!File.Exists(AppPaths.StateFile))
        {
            return new AppState();
        }

        try
        {
            var json = File.ReadAllText(AppPaths.StateFile);
            var state = JsonSerializer.Deserialize<AppState>(json, Options) ?? new AppState();
            if (state.Settings.LibraryRoots.Count == 0)
            {
                state.Settings.LibraryRoots = ReadLegacyRoots(json);
            }

            return state;
        }
        catch
        {
            BackupCorruptState();
            return new AppState();
        }
    }

    public void Save(AppState state)
    {
        Directory.CreateDirectory(AppPaths.BaseFolder);
        var json = JsonSerializer.Serialize(state, Options);
        File.WriteAllText(AppPaths.StateFile, json);
    }

    public void SaveLibrary(AppState state, IReadOnlyCollection<MediaItem> items)
    {
        state.MediaItems = items.OrderBy(item => item.Title, StringComparer.OrdinalIgnoreCase).ToList();
        Save(state);
    }

    private static List<LibraryRoot> ReadLegacyRoots(string json)
    {
        var result = new List<LibraryRoot>();
        var root = JsonNode.Parse(json);
        var legacyRoots = root?["Settings"]?["LibraryRoots"]?.AsArray();
        if (legacyRoots is null)
        {
            return result;
        }

        foreach (var entry in legacyRoots)
        {
            if (entry is null)
            {
                continue;
            }

            if (entry.GetValueKind() == JsonValueKind.String)
            {
                var path = entry.GetValue<string>();
                if (!string.IsNullOrWhiteSpace(path))
                {
                    result.Add(new LibraryRoot
                    {
                        Path = path,
                        DisplayName = Path.GetFileName(path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)),
                        Kind = LibraryRootKind.Series
                    });
                }
            }
        }

        return result;
    }

    private static void BackupCorruptState()
    {
        if (!File.Exists(AppPaths.StateFile))
        {
            return;
        }

        var corruptPath = AppPaths.StateFile + ".corrupt";
        File.Delete(corruptPath);
        File.Move(AppPaths.StateFile, corruptPath);
    }
}
