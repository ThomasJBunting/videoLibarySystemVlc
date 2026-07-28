namespace VideoLibrarySystemVlc.Models;

public sealed class AppSettings
{
    public List<LibraryRoot> LibraryRoots { get; set; } = [];
    public string? VlcExecutablePath { get; set; }
}
