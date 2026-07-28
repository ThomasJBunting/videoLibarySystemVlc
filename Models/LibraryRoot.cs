using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace VideoLibrarySystemVlc.Models;

public sealed class LibraryRoot : INotifyPropertyChanged
{
    private string path = string.Empty;
    private string displayName = string.Empty;

    public string Path
    {
        get => path;
        set => SetField(ref path, value);
    }

    public LibraryRootKind Kind { get; set; }

    public string DisplayName
    {
        get => displayName;
        set => SetField(ref displayName, value);
    }

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
