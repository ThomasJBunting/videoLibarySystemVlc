namespace VideoLibrarySystemVlc.Effects;

/// <summary>
/// WPF ShaderEffect that applies a holographic foil effect to card edges.
/// Creates a dynamic rainbow shimmer with specular highlights on the card borders.
/// </summary>
public class HoloFoilEffect : ShaderEffect
{
	private static readonly PixelShader Shader = new PixelShader
	{
		UriSource = new Uri("pack://application:,,,/Shaders/HoloFoilEffect.ps")
	};

	/// <summary>
	/// Rainbow spectrum texture sampler (register s1).
	/// </summary>
	public static readonly DependencyProperty RainbowBrushProperty =
		RegisterPixelShaderSamplerProperty("RainbowBrush", typeof(HoloFoilEffect), 1);

	/// <summary>
	/// Mouse tilt angle vector (register c0).
	/// X and Y range from -1.0 to 1.0 based on mouse position.
	/// </summary>
	public static readonly DependencyProperty TiltAngleProperty =
		DependencyProperty.Register("TiltAngle", typeof(System.Windows.Point), typeof(HoloFoilEffect),
			new UIPropertyMetadata(new System.Windows.Point(0, 0), PixelShaderConstantCallback(0)));

	/// <summary>
	/// Animated time for subtle rainbow movement (register c1).
	/// </summary>
	public static readonly DependencyProperty TimeProperty =
		DependencyProperty.Register("Time", typeof(double), typeof(HoloFoilEffect),
			new UIPropertyMetadata(0.0, PixelShaderConstantCallback(1)));

	public HoloFoilEffect()
	{
		PixelShader = Shader;
		UpdateShaderValue(RainbowBrushProperty);
		UpdateShaderValue(TiltAngleProperty);
		UpdateShaderValue(TimeProperty);
	}

	/// <summary>
	/// Gets or sets the rainbow spectrum texture brush.
	/// </summary>
	public System.Windows.Media.Brush RainbowBrush
	{
		get => (System.Windows.Media.Brush)GetValue(RainbowBrushProperty);
		set => SetValue(RainbowBrushProperty, value);
	}

	/// <summary>
	/// Gets or sets the tilt angle from mouse movement.
	/// </summary>
	public System.Windows.Point TiltAngle
	{
		get => (System.Windows.Point)GetValue(TiltAngleProperty);
		set => SetValue(TiltAngleProperty, value);
	}

	/// <summary>
	/// Gets or sets the animation time.
	/// </summary>
	public double Time
	{
		get => (double)GetValue(TimeProperty);
		set => SetValue(TimeProperty, value);
	}
}
