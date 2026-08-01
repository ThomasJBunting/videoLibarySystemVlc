using VideoLibrarySystemVlc.Effects;
using System.Windows.Media.Imaging;

namespace VideoLibrarySystemVlc.Controls;

/// <summary>
/// A container control that applies 3D parallax tilt and water shimmer effects to card content.
/// Supports multi-layer depth with separate background, image, foil, and foreground layers.
/// </summary>
public partial class EffectCardContainer : System.Windows.Controls.UserControl
{
	private WaterShimmerEffect? _waterEffect;
	private WriteableBitmap? _noiseTexture;
	private bool _isAnimating;

	public EffectCardContainer()
	{
		InitializeComponent();
		Loaded += OnLoaded;
		Unloaded += OnUnloaded;
	}

	#region Dependency Properties

	public static readonly DependencyProperty EnableTiltEffectProperty =
		DependencyProperty.Register(
			nameof(EnableTiltEffect),
			typeof(bool),
			typeof(EffectCardContainer),
			new PropertyMetadata(true));

	public static readonly DependencyProperty EnableShimmerEffectProperty =
		DependencyProperty.Register(
			nameof(EnableShimmerEffect),
			typeof(bool),
			typeof(EffectCardContainer),
			new PropertyMetadata(false, OnEnableShimmerEffectChanged));

	public static readonly DependencyProperty EnableFoilEffectProperty =
		DependencyProperty.Register(
			nameof(EnableFoilEffect),
			typeof(bool),
			typeof(EffectCardContainer),
			new PropertyMetadata(false, OnEnableFoilEffectChanged));

	public static readonly DependencyProperty BackgroundContentProperty =
		DependencyProperty.Register(
			nameof(BackgroundContent),
			typeof(object),
			typeof(EffectCardContainer),
			new PropertyMetadata(null));

	public static readonly DependencyProperty ImageContentProperty =
		DependencyProperty.Register(
			nameof(ImageContent),
			typeof(object),
			typeof(EffectCardContainer),
			new PropertyMetadata(null));

	public static readonly DependencyProperty ForegroundContentProperty =
		DependencyProperty.Register(
			nameof(ForegroundContent),
			typeof(object),
			typeof(EffectCardContainer),
			new PropertyMetadata(null));

	public static readonly DependencyProperty CornerRadiusProperty =
		DependencyProperty.Register(
			nameof(CornerRadius),
			typeof(CornerRadius),
			typeof(EffectCardContainer),
			new PropertyMetadata(new CornerRadius(12)));

	/// <summary>
	/// Gets or sets whether the 3D tilt effect is enabled.
	/// </summary>
	public bool EnableTiltEffect
	{
		get => (bool)GetValue(EnableTiltEffectProperty);
		set => SetValue(EnableTiltEffectProperty, value);
	}

	/// <summary>
	/// Gets or sets whether the water shimmer effect is enabled.
	/// </summary>
	public bool EnableShimmerEffect
	{
		get => (bool)GetValue(EnableShimmerEffectProperty);
		set => SetValue(EnableShimmerEffectProperty, value);
	}

	/// <summary>
	/// Gets or sets whether the rainbow foil effect is enabled.
	/// </summary>
	public bool EnableFoilEffect
	{
		get => (bool)GetValue(EnableFoilEffectProperty);
		set => SetValue(EnableFoilEffectProperty, value);
	}

	/// <summary>
	/// Gets or sets the background layer content (lowest depth).
	/// </summary>
	public object BackgroundContent
	{
		get => GetValue(BackgroundContentProperty);
		set => SetValue(BackgroundContentProperty, value);
	}

	/// <summary>
	/// Gets or sets the image layer content (middle depth, receives shimmer effect).
	/// </summary>
	public object ImageContent
	{
		get => GetValue(ImageContentProperty);
		set => SetValue(ImageContentProperty, value);
	}

	/// <summary>
	/// Gets or sets the foreground layer content (highest depth).
	/// </summary>
	public object ForegroundContent
	{
		get => GetValue(ForegroundContentProperty);
		set => SetValue(ForegroundContentProperty, value);
	}

	/// <summary>
	/// Gets or sets the corner radius of the card border.
	/// </summary>
	public new CornerRadius CornerRadius
	{
		get => (CornerRadius)GetValue(CornerRadiusProperty);
		set => SetValue(CornerRadiusProperty, value);
	}

	#endregion

	private void OnLoaded(object sender, RoutedEventArgs e)
	{
		if (EnableShimmerEffect)
		{
			InitializeShimmerEffect();
		}

		if (EnableFoilEffect)
		{
			InitializeFoilEffect();
		}
	}

	private void OnUnloaded(object sender, RoutedEventArgs e)
	{
		_isAnimating = false;
		CompositionTarget.Rendering -= OnRendering;
	}

	private static void OnEnableShimmerEffectChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		if (d is EffectCardContainer container && container.IsLoaded)
		{
			if ((bool)e.NewValue)
			{
				container.InitializeShimmerEffect();
			}
			else
			{
				container.RemoveShimmerEffect();
			}
		}
	}

	private static void OnEnableFoilEffectChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		if (d is EffectCardContainer container && container.IsLoaded)
		{
			if ((bool)e.NewValue)
			{
				container.InitializeFoilEffect();
			}
			else
			{
				container.FoilLayer.Opacity = 0;
			}
		}
	}

	private void InitializeShimmerEffect()
	{
		try
		{
			// Generate noise texture if not already created
			if (_noiseTexture == null)
			{
				_noiseTexture = NoiseTextureGenerator.GenerateNoiseTexture(256, 256, 0.1);
			}

			// Create shimmer effect
			_waterEffect = new WaterShimmerEffect
			{
				NoiseMap = new ImageBrush(_noiseTexture)
				{
					TileMode = TileMode.Tile,
					Viewport = new Rect(0, 0, 1, 1),
					ViewportUnits = BrushMappingMode.RelativeToBoundingBox
				}
			};

			// Apply to image layer
			ImageLayer.Effect = _waterEffect;

			// Start animation loop
			if (!_isAnimating)
			{
				_isAnimating = true;
				CompositionTarget.Rendering += OnRendering;
			}
		}
		catch (Exception)
		{
			// Silently fail if shader compilation or loading fails
			// The card will still display without the shimmer effect
		}
	}

	private void RemoveShimmerEffect()
	{
		ImageLayer.Effect = null;
		_waterEffect = null;

		if (!EnableFoilEffect)
		{
			_isAnimating = false;
			CompositionTarget.Rendering -= OnRendering;
		}
	}

	private void InitializeFoilEffect()
	{
		// Animate foil layer opacity
		var opacityAnimation = new DoubleAnimation
		{
			From = 0.35,
			To = 0.5,
			Duration = TimeSpan.FromSeconds(2),
			AutoReverse = true,
			RepeatBehavior = RepeatBehavior.Forever
		};

		FoilLayer.BeginAnimation(OpacityProperty, opacityAnimation);

		// Animate gradient rotation
		var rotateTransform = new RotateTransform(0, 0.5, 0.5);
		RainbowBrush.RelativeTransform = rotateTransform;

		var rotationAnimation = new DoubleAnimation
		{
			From = 0,
			To = 360,
			Duration = TimeSpan.FromSeconds(8),
			RepeatBehavior = RepeatBehavior.Forever
		};

		rotateTransform.BeginAnimation(RotateTransform.AngleProperty, rotationAnimation);
	}

	private void OnRendering(object? sender, EventArgs e)
	{
		if (_waterEffect != null && EnableShimmerEffect)
		{
			// Increment time for shader animation (~60 FPS)
			_waterEffect.Time += 0.016;
		}
	}
}
