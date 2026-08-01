using System.Windows.Media.Imaging;

namespace VideoLibrarySystemVlc.Effects;

/// <summary>
/// Generates procedural noise textures for shader effects.
/// </summary>
public static class NoiseTextureGenerator
{
	/// <summary>
	/// Generates a seamless tileable Perlin-like noise texture.
	/// </summary>
	/// <param name="width">Width of the texture in pixels</param>
	/// <param name="height">Height of the texture in pixels</param>
	/// <param name="scale">Scale of the noise (higher = more detailed)</param>
	/// <param name="seed">Random seed for reproducible noise</param>
	/// <returns>A WriteableBitmap containing the noise texture</returns>
	public static WriteableBitmap GenerateNoiseTexture(int width = 256, int height = 256, double scale = 0.1, int seed = 42)
	{
		var bitmap = new WriteableBitmap(width, height, 96, 96, System.Windows.Media.PixelFormats.Bgra32, null);
		var pixels = new byte[width * height * 4];
		var random = new Random(seed);

		// Generate multiple octaves of noise for better variety
		for (int y = 0; y < height; y++)
		{
			for (int x = 0; x < width; x++)
			{
				// Multi-octave noise
				double value = 0.0;
				double amplitude = 1.0;
				double frequency = scale;

				for (int octave = 0; octave < 4; octave++)
				{
					value += SimplexNoise(x * frequency, y * frequency, seed + octave) * amplitude;
					amplitude *= 0.5;
					frequency *= 2.0;
				}

				// Normalize to 0-255 range
				byte noiseValue = (byte)((value * 0.5 + 0.5) * 255);

				int index = (y * width + x) * 4;
				pixels[index] = noiseValue;     // B
				pixels[index + 1] = noiseValue; // G
				pixels[index + 2] = noiseValue; // R
				pixels[index + 3] = 255;        // A
			}
		}

		bitmap.WritePixels(new System.Windows.Int32Rect(0, 0, width, height), pixels, width * 4, 0);
		return bitmap;
	}

	/// <summary>
	/// Simplified 2D noise function (similar to Perlin/Simplex noise)
	/// </summary>
	private static double SimplexNoise(double x, double y, int seed)
	{
		// Grid cell coordinates
		int xi = (int)Math.Floor(x);
		int yi = (int)Math.Floor(y);

		// Fractional parts
		double xf = x - xi;
		double yf = y - yi;

		// Fade curves
		double u = Fade(xf);
		double v = Fade(yf);

		// Hash corners
		int aa = Hash(Hash(xi, seed) + yi, seed);
		int ab = Hash(Hash(xi, seed) + yi + 1, seed);
		int ba = Hash(Hash(xi + 1, seed) + yi, seed);
		int bb = Hash(Hash(xi + 1, seed) + yi + 1, seed);

		// Gradients
		double x1 = Lerp(Gradient(aa, xf, yf), Gradient(ba, xf - 1, yf), u);
		double x2 = Lerp(Gradient(ab, xf, yf - 1), Gradient(bb, xf - 1, yf - 1), u);

		return Lerp(x1, x2, v);
	}

	private static int Hash(int value, int seed)
	{
		value = (value ^ seed) * 0x45d9f3b;
		value = (value >> 16) ^ value;
		value = value * unchecked((int)0x85ebca6b);
		value = (value >> 13) ^ value;
		value = value * unchecked((int)0xc2b2ae35);
		return (value >> 16) ^ value;
	}

	private static double Gradient(int hash, double x, double y)
	{
		// Convert hash to gradient direction
		int h = hash & 7;
		double u = h < 4 ? x : y;
		double v = h < 4 ? y : x;
		return ((h & 1) == 0 ? u : -u) + ((h & 2) == 0 ? v : -v);
	}

	private static double Lerp(double a, double b, double t)
	{
		return a + t * (b - a);
	}

	private static double Fade(double t)
	{
		// Smoothstep interpolation (6t^5 - 15t^4 + 10t^3)
		return t * t * t * (t * (t * 6 - 15) + 10);
	}
}
