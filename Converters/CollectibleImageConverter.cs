using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace VideoLibrarySystemVlc.Converters;

public sealed class CollectibleImageConverter : IMultiValueConverter
{
	public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
	{
		if (values is null || values.Length < 2)
		{
			return null;
		}

		var imagePath = values[0] as string;
		var isCollected = values[1] is bool collected && collected;

		if (string.IsNullOrWhiteSpace(imagePath))
		{
			return null;
		}

		var image = TryLoadBitmap(imagePath);
		if (image is null)
		{
			return null;
		}

		if (!isCollected)
		{
			var grayscale = new FormatConvertedBitmap();
			grayscale.BeginInit();
			grayscale.Source = image;
			grayscale.DestinationFormat = PixelFormats.Gray8;
			grayscale.EndInit();
			grayscale.Freeze();
			return grayscale;
		}

		return image;
	}

	public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) => throw new NotSupportedException();

	private static BitmapImage? TryLoadBitmap(string imagePath)
	{
		if (imagePath.StartsWith("pack://", StringComparison.OrdinalIgnoreCase))
		{
			try
			{
				var uri = new Uri(imagePath, UriKind.Absolute);
				var resourceStream = System.Windows.Application.GetResourceStream(uri);
				if (resourceStream?.Stream is null)
				{
					return null;
				}

				var image = new BitmapImage();
				image.BeginInit();
				image.CacheOption = BitmapCacheOption.OnLoad;
				image.StreamSource = resourceStream.Stream;
				image.EndInit();
				image.Freeze();
				return image;
			}
			catch
			{
				return null;
			}
		}

		if (!File.Exists(imagePath))
		{
			return null;
		}

		var bitmap = new BitmapImage();
		bitmap.BeginInit();
		bitmap.CacheOption = BitmapCacheOption.OnLoad;
		bitmap.UriSource = new Uri(imagePath, UriKind.Absolute);
		bitmap.EndInit();
		bitmap.Freeze();
		return bitmap;
	}
}
