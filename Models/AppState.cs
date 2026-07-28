namespace VideoLibrarySystemVlc.Models;

public sealed class AppState
{
    public AppSettings Settings { get; set; } = new();
    public List<MediaItem> MediaItems { get; set; } = [];
    public PlaybackSnapshot Playback { get; set; } = new();
}
