namespace VideoLibrarySystemVlc.Models;

public sealed class RecentSeriesPlay
{
    public string SeriesItemId { get; set; } = string.Empty;
    public string EpisodePath { get; set; } = string.Empty;
    public DateTime PlayedUtc { get; set; }
}
