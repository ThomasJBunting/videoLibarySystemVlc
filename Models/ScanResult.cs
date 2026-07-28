namespace VideoLibrarySystemVlc.Models;

public sealed class ScanResult
{
    public required List<MediaItem> Items { get; init; }
    public List<string> SkippedRoots { get; init; } = [];
}
