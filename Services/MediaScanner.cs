using System.Text.RegularExpressions;
using VideoLibrarySystemVlc.Models;

namespace VideoLibrarySystemVlc.Services;

public sealed class MediaScanner
{
    private static readonly string[] VideoExtensions =
    [
        ".mp4", ".mkv", ".avi", ".mov", ".wmv", ".m4v", ".webm", ".ts"
    ];

    private static readonly EnumerationOptions RecursiveOptions = new()
    {
        RecurseSubdirectories = true,
        IgnoreInaccessible = true,
        ReturnSpecialDirectories = false
    };

    private static readonly Regex SeasonFolderPattern = new(@"^(season|series|s)\s*(\d+)$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex EpisodePattern = new(@"(?:^|[^\d])(?:s(?<season>\d{1,2})e(?<episode>\d{1,3})|(?<episode>\d{1,3})x(?<altseason>\d{1,2}))", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex SeasonPattern = new(@"(?:season\s*(?<season>\d{1,2})|s(?<season>\d{1,2}))", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public ScanResult Scan(IReadOnlyCollection<LibraryRoot> roots, IReadOnlyCollection<MediaItem> previousItems)
    {
        var previousById = previousItems.ToDictionary(item => item.Id, StringComparer.OrdinalIgnoreCase);
        var items = new List<MediaItem>();
        var skippedRoots = new List<string>();

        foreach (var root in roots.Where(root => Directory.Exists(root.Path)))
        {
            var scannedItems = root.Kind == LibraryRootKind.Series
                ? ScanSeriesRoot(root, skippedRoots)
                : ScanMovieRoot(root, skippedRoots);

            items.AddRange(scannedItems);
        }

        foreach (var item in items)
        {
            if (previousById.TryGetValue(item.Id, out var previous))
            {
                item.Title = string.IsNullOrWhiteSpace(previous.Title) ? item.Title : previous.Title;
                item.ArtworkPath = previous.ArtworkPath;
                item.LastScannedUtc = previous.LastScannedUtc;
            }
        }

        return new ScanResult
        {
            Items = items.OrderBy(item => item.Kind).ThenBy(item => item.Title, StringComparer.OrdinalIgnoreCase).ToList(),
            SkippedRoots = skippedRoots
        };
    }

    private static IEnumerable<MediaItem> ScanMovieRoot(LibraryRoot root, ICollection<string> skippedRoots)
    {
        foreach (var file in EnumerateVideoFiles(root.Path, skippedRoots).OrderBy(NaturalSortKey))
        {
            yield return BuildMovieItem(root.Path, file);
        }
    }

    private static IEnumerable<MediaItem> ScanSeriesRoot(LibraryRoot root, ICollection<string> skippedRoots)
    {
        var files = EnumerateVideoFiles(root.Path, skippedRoots);
        if (files.Count == 0)
        {
            yield break;
        }

        var groups = files.GroupBy(file => GetSeriesGroupKey(root.Path, file), StringComparer.OrdinalIgnoreCase);
        foreach (var group in groups)
        {
            var orderedFiles = group
                .OrderBy(file => EpisodeSortHint(file).Season)
                .ThenBy(file => EpisodeSortHint(file).Episode)
                .ThenBy(NaturalSortKey)
                .ToList();

            yield return BuildSeriesItem(root.Path, group.Key, orderedFiles);
        }
    }

    private static List<string> EnumerateVideoFiles(string rootPath, ICollection<string> skippedRoots)
    {
        var files = new List<string>();
        try
        {
            foreach (var file in Directory.EnumerateFiles(rootPath, "*", RecursiveOptions))
            {
                if (IsVideoFile(file))
                {
                    files.Add(file);
                }
            }
        }
        catch (UnauthorizedAccessException)
        {
            skippedRoots.Add(rootPath);
        }
        catch (IOException)
        {
            skippedRoots.Add(rootPath);
        }

        return files;
    }

    private static MediaItem BuildMovieItem(string rootPath, string file)
    {
        var title = Path.GetFileNameWithoutExtension(file);
        return new MediaItem
        {
            Id = StableId(rootPath, file),
            Title = title,
            Kind = MediaKind.Movie,
            RootPath = rootPath,
            ContainerPath = Path.GetDirectoryName(file) ?? rootPath,
            PrimaryPath = file,
            PlaylistPaths = [file],
            EpisodeCount = 1,
            LastScannedUtc = DateTime.UtcNow
        };
    }

    private static MediaItem BuildSeriesItem(string rootPath, string seriesFolder, IReadOnlyCollection<string> files)
    {
        var orderedFiles = files.ToList();
        return new MediaItem
        {
            Id = StableId(rootPath, seriesFolder),
            Title = Path.GetFileName(seriesFolder),
            Kind = MediaKind.Series,
            RootPath = rootPath,
            ContainerPath = seriesFolder,
            PrimaryPath = orderedFiles[0],
            PlaylistPaths = orderedFiles,
            EpisodeCount = orderedFiles.Count,
            LastScannedUtc = DateTime.UtcNow
        };
    }

    private static string GetSeriesGroupKey(string rootPath, string filePath)
    {
        var relative = Path.GetRelativePath(rootPath, filePath);
        var segments = relative.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries);

        if (segments.Length <= 1)
        {
            return Path.GetDirectoryName(filePath) ?? rootPath;
        }

        return Path.Combine(rootPath, segments[0]);
    }

    private static (int Season, int Episode) EpisodeSortHint(string file)
    {
        var name = Path.GetFileNameWithoutExtension(file);
        var match = EpisodePattern.Match(name);
        if (match.Success)
        {
            var seasonText = match.Groups["season"].Success ? match.Groups["season"].Value : match.Groups["altseason"].Value;
            return (ParseInt(seasonText), ParseInt(match.Groups["episode"].Value));
        }

        var seasonMatch = SeasonPattern.Match(name);
        return seasonMatch.Success ? (ParseInt(seasonMatch.Groups["season"].Value), 0) : (0, 0);
    }

    private static int ParseInt(string? text) => int.TryParse(text, out var value) ? value : 0;

    private static string StableId(string root, string value) =>
        Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes($"{root}|{value}")));

    private static bool IsVideoFile(string file) => VideoExtensions.Contains(Path.GetExtension(file), StringComparer.OrdinalIgnoreCase);

    private static string NaturalSortKey(string path)
    {
        var name = Path.GetFileNameWithoutExtension(path);
        return Regex.Replace(name, @"\d+", m => m.Value.PadLeft(8, '0'));
    }
}
