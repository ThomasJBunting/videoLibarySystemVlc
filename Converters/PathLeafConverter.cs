using System.Globalization;
using System.Windows.Data;

namespace VideoLibrarySystemVlc.Converters;

public sealed class PathLeafConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var text = value as string;
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        return Path.GetFileName(text.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotSupportedException();
}
