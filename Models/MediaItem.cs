using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace VideoLibrarySystemVlc.Models;

public sealed class MediaItem : INotifyPropertyChanged
{
    private string title = string.Empty;
    private string? artworkPath;

    public string Id { get; set; } = string.Empty;

    public string Title
    {
        get => title;
        set => SetField(ref title, value);
    }

    public MediaKind Kind { get; set; }
    public string RootPath { get; set; } = string.Empty;
    public string ContainerPath { get; set; } = string.Empty;
    public string PrimaryPath { get; set; } = string.Empty;
    public List<string> PlaylistPaths { get; set; } = [];

    public string? ArtworkPath
    {
        get => artworkPath;
        set => SetField(ref artworkPath, value);
    }

    public int? SeasonNumber { get; set; }
    public int? EpisodeNumber { get; set; }
    public int EpisodeCount { get; set; }
    public DateTime LastScannedUtc { get; set; }

    public event PropertyChangedEventHandler? PropertyChanged;

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        return true;
    }
}
