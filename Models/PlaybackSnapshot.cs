namespace VideoLibrarySystemVlc.Models;

public sealed class PlaybackSnapshot
{
    public string? LastItemId { get; set; }
    public string? LastFilePath { get; set; }
    public int? LastKnownTimeSeconds { get; set; }
    public DateTime? LastStartedUtc { get; set; }
    public List<RecentSeriesPlay> RecentSeriesPlays { get; set; } = [];
}
