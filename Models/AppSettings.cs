namespace VideoLibrarySystemVlc.Models;

public sealed class AppSettings
{
    public List<LibraryRoot> LibraryRoots { get; set; } = [];
    public string? VlcExecutablePath { get; set; }

    // Back Rooms settings
    public string? CollectiblesSourceUrl { get; set; }
    public string? TickerReviewsUrl { get; set; }
    public bool TickerTapeEnabled { get; set; } = true;
    public string? LateFeeUrl { get; set; } = "https://www.google.com";
}
