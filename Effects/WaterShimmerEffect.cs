namespace VideoLibrarySystemVlc.Effects;

/// <summary>
/// WPF ShaderEffect wrapper for the WaterShimmer HLSL shader.
/// Provides a subtle, animated water-like distortion effect inspired by Wind Waker.
/// </summary>
public class WaterShimmerEffect : ShaderEffect
{
	private static readonly PixelShader _pixelShader = new()
	{
		UriSource = new Uri("pack://application:,,,/Shaders/WaterShimmer.ps", UriKind.Absolute)
	};

	public static readonly DependencyProperty InputProperty =
		ShaderEffect.RegisterPixelShaderSamplerProperty(nameof(Input), typeof(WaterShimmerEffect), 0);

	public static readonly DependencyProperty NoiseMapProperty =
		ShaderEffect.RegisterPixelShaderSamplerProperty(nameof(NoiseMap), typeof(WaterShimmerEffect), 1);

	public static readonly DependencyProperty TimeProperty =
		DependencyProperty.Register(
			nameof(Time),
			typeof(double),
			typeof(WaterShimmerEffect),
			new UIPropertyMetadata(0.0, PixelShaderConstantCallback(0)));

	public WaterShimmerEffect()
	{
		PixelShader = _pixelShader;
		UpdateShaderValue(InputProperty);
		UpdateShaderValue(NoiseMapProperty);
		UpdateShaderValue(TimeProperty);
	}

	/// <summary>
	/// The input texture to apply the shimmer effect to.
	/// </summary>
	public System.Windows.Media.Brush Input
	{
		get => (System.Windows.Media.Brush)GetValue(InputProperty);
		set => SetValue(InputProperty, value);
	}

	/// <summary>
	/// The noise texture used to generate wave distortions.
	/// Should be a seamless tileable noise texture.
	/// </summary>
	public System.Windows.Media.Brush NoiseMap
	{
		get => (System.Windows.Media.Brush)GetValue(NoiseMapProperty);
		set => SetValue(NoiseMapProperty, value);
	}

	/// <summary>
	/// The animation time parameter. Increment this each frame to animate the shimmer.
	/// Recommended increment: 0.016 per frame (~60 FPS).
	/// </summary>
	public double Time
	{
		get => (double)GetValue(TimeProperty);
		set => SetValue(TimeProperty, value);
	}
}
