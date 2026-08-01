using SysWin = System.Windows;

namespace VideoLibrarySystemVlc.Behaviors;

/// <summary>
/// Attached behavior that applies a 3D parallax tilt effect to a card based on mouse position.
/// The card tilts toward the cursor as it moves over the element, creating a depth effect.
/// </summary>
public static class Card3DEffectBehavior
{
	private const double MaxTiltAngle = 15.0; // Maximum tilt angle in degrees
	private const double AnimationDuration = 0.15; // Smooth animation duration in seconds

	private static readonly Dictionary<FrameworkElement, TiltState> _tiltStates = new();

	private class TiltState
	{
		public bool IsMouseOver { get; set; }
		public SysWin.Point LastMousePosition { get; set; }
	}

	#region Attached Property: IsEnabled

	public static readonly DependencyProperty IsEnabledProperty =
		DependencyProperty.RegisterAttached(
			"IsEnabled",
			typeof(bool),
			typeof(Card3DEffectBehavior),
			new PropertyMetadata(false, OnIsEnabledChanged));

	public static bool GetIsEnabled(DependencyObject obj)
		=> (bool)obj.GetValue(IsEnabledProperty);

	public static void SetIsEnabled(DependencyObject obj, bool value)
		=> obj.SetValue(IsEnabledProperty, value);

	private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		if (d is not FrameworkElement element)
			return;

		if ((bool)e.NewValue)
		{
			element.Loaded += OnElementLoaded;
			element.MouseEnter += OnMouseEnter;
			element.MouseLeave += OnMouseLeave;
			element.MouseMove += OnMouseMove;
			element.Unloaded += OnElementUnloaded;

			_tiltStates[element] = new TiltState();
		}
		else
		{
			element.Loaded -= OnElementLoaded;
			element.MouseEnter -= OnMouseEnter;
			element.MouseLeave -= OnMouseLeave;
			element.MouseMove -= OnMouseMove;
			element.Unloaded -= OnElementUnloaded;

			_tiltStates.Remove(element);
		}
	}

	#endregion

	private static void OnElementLoaded(object sender, RoutedEventArgs e)
	{
		if (sender is not FrameworkElement element)
			return;

		// Ensure the element has a 3D transform group set up
		if (element.RenderTransform is not TransformGroup)
		{
			element.RenderTransform = new TransformGroup
			{
				Children =
				{
					new RotateTransform(),
					new ScaleTransform(),
					new TranslateTransform()
				}
			};
		}

		// Set transform origin to center for proper rotation
		element.RenderTransformOrigin = new SysWin.Point(0.5, 0.5);
	}

	private static void OnElementUnloaded(object sender, RoutedEventArgs e)
	{
		if (sender is FrameworkElement element)
		{
			_tiltStates.Remove(element);
		}
	}

	private static void OnMouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
	{
		if (sender is FrameworkElement element && _tiltStates.TryGetValue(element, out var state))
		{
			state.IsMouseOver = true;
		}
	}

	private static void OnMouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
	{
		if (sender is FrameworkElement element && _tiltStates.TryGetValue(element, out var state))
		{
			state.IsMouseOver = false;
			AnimateTiltToNeutral(element);
		}
	}

	private static void OnMouseMove(object sender, System.Windows.Input.MouseEventArgs e)
	{
		if (sender is not FrameworkElement element)
			return;

		if (!GetIsEnabled(element))
			return;

		if (!_tiltStates.TryGetValue(element, out var state) || !state.IsMouseOver)
			return;

		state.LastMousePosition = e.GetPosition(element);
		UpdateTilt(element, state.LastMousePosition);
	}

	private static void UpdateTilt(FrameworkElement element, SysWin.Point mousePosition)
	{
		double width = element.ActualWidth;
		double height = element.ActualHeight;

		if (width == 0 || height == 0)
			return;

		// Calculate normalized position (-1 to 1 range)
		double centerX = width / 2;
		double centerY = height / 2;
		double normalizedX = (mousePosition.X - centerX) / centerX;
		double normalizedY = -(mousePosition.Y - centerY) / centerY; // Invert Y for intuitive tilt

		// Calculate rotation angles
		double rotationY = normalizedX * MaxTiltAngle;
		double rotationX = normalizedY * MaxTiltAngle;

		// Apply the tilt animation
		AnimateTilt(element, rotationX, rotationY);
	}

	private static void AnimateTilt(FrameworkElement element, double rotationX, double rotationY)
	{
		if (element.RenderTransform is not TransformGroup transformGroup)
			return;

		var rotateTransform = transformGroup.Children.OfType<RotateTransform>().FirstOrDefault();
		if (rotateTransform == null)
			return;

		// Create a composite transform that simulates 3D rotation
		// We'll use RotateTransform and SkewTransform to approximate 3D perspective
		var duration = TimeSpan.FromSeconds(AnimationDuration);

		// Animate rotation (this is a 2D approximation of 3D tilt)
		var angleAnimation = new DoubleAnimation
		{
			To = rotationY * 0.3, // Scale down for subtlety
			Duration = duration,
			EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
		};

		rotateTransform.BeginAnimation(RotateTransform.AngleProperty, angleAnimation);

		// Add a slight scale effect for depth
		var scaleTransform = transformGroup.Children.OfType<ScaleTransform>().FirstOrDefault();
		if (scaleTransform != null)
		{
			var scaleAnimation = new DoubleAnimation
			{
				To = 1.02, // Slight zoom
				Duration = duration,
				EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
			};

			scaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, scaleAnimation);
			scaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, scaleAnimation);
		}
	}

	private static void AnimateTiltToNeutral(FrameworkElement element)
	{
		if (element.RenderTransform is not TransformGroup transformGroup)
			return;

		var duration = TimeSpan.FromSeconds(AnimationDuration * 1.5);

		// Reset rotation
		var rotateTransform = transformGroup.Children.OfType<RotateTransform>().FirstOrDefault();
		if (rotateTransform != null)
		{
			var angleAnimation = new DoubleAnimation
			{
				To = 0,
				Duration = duration,
				EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
			};
			rotateTransform.BeginAnimation(RotateTransform.AngleProperty, angleAnimation);
		}

		// Reset scale
		var scaleTransform = transformGroup.Children.OfType<ScaleTransform>().FirstOrDefault();
		if (scaleTransform != null)
		{
			var scaleAnimation = new DoubleAnimation
			{
				To = 1.0,
				Duration = duration,
				EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
			};
			scaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, scaleAnimation);
			scaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, scaleAnimation);
		}
	}
}
