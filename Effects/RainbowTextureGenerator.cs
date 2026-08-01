using System.Windows.Media.Imaging;

namespace VideoLibrarySystemVlc.Effects;

/// <summary>
/// Generates a procedural rainbow spectrum texture for the holographic foil effect.
/// </summary>
public static class RainbowTextureGenerator
{
	/// <summary>
	/// Creates a horizontal rainbow gradient texture.
	/// </summary>
	public static BitmapSource GenerateRainbowTexture(int width = 256, int height = 16)
	{
		var bitmap = new WriteableBitmap(width, height, 96, 96, System.Windows.Media.PixelFormats.Bgra32, null);

		int bytesPerPixel = 4;
		int stride = width * bytesPerPixel;
		byte[] pixels = new byte[height * stride];

		for (int y = 0; y < height; y++)
		{
			for (int x = 0; x < width; x++)
			{
				float t = x / (float)(width - 1);

				// Convert HSV to RGB for smooth rainbow
				var (r, g, b) = HsvToRgb(t * 360.0f, 1.0f, 1.0f);

				int pixelOffset = y * stride + x * bytesPerPixel;
				pixels[pixelOffset + 0] = b; // Blue
				pixels[pixelOffset + 1] = g; // Green
				pixels[pixelOffset + 2] = r; // Red
				pixels[pixelOffset + 3] = 255; // Alpha
			}
		}

		bitmap.WritePixels(new System.Windows.Int32Rect(0, 0, width, height), pixels, stride, 0);
		bitmap.Freeze();
		return bitmap;
	}

	/// <summary>
	/// Converts HSV color space to RGB.
	/// </summary>
	private static (byte r, byte g, byte b) HsvToRgb(float h, float s, float v)
	{
		// H: 0-360, S: 0-1, V: 0-1
		float c = v * s;
		float x = c * (1 - Math.Abs((h / 60.0f) % 2 - 1));
		float m = v - c;

		float r1, g1, b1;

		if (h < 60)
		{
			r1 = c; g1 = x; b1 = 0;
		}
		else if (h < 120)
		{
			r1 = x; g1 = c; b1 = 0;
		}
		else if (h < 180)
		{
			r1 = 0; g1 = c; b1 = x;
		}
		else if (h < 240)
		{
			r1 = 0; g1 = x; b1 = c;
		}
		else if (h < 300)
		{
			r1 = x; g1 = 0; b1 = c;
		}
		else
		{
			r1 = c; g1 = 0; b1 = x;
		}

		return (
			(byte)((r1 + m) * 255),
			(byte)((g1 + m) * 255),
			(byte)((b1 + m) * 255)
		);
	}
}
