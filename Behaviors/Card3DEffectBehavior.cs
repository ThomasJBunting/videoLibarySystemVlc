using SysWin = System.Windows;

namespace VideoLibrarySystemVlc.Behaviors;

/// <summary>
/// Attached behavior that applies a 3D perspective tilt effect to a card based on mouse position.
/// The card tilts toward the cursor as it moves over the element, creating a compelling parallax depth effect.
/// Uses perspective transforms (skew/rotation combination) for proper X/Y axis tilt (not Z-axis spin).
/// </summary>
public static class Card3DEffectBehavior
{
	private const double MaxTiltAngle = 15.0; // Maximum tilt angle in degrees for perspective
	private const double AnimationDuration = 0.15; // Smooth animation duration in seconds

	private static readonly Dictionary<FrameworkElement, TiltState> _tiltStates = new();

	private class TiltState
	{
		public bool IsMouseOver { get; set; }
		public SysWin.Point LastMousePosition { get; set; }
		public double CurrentRotationX { get; set; } // X-axis rotation in degrees
		public double CurrentRotationY { get; set; } // Y-axis rotation in degrees
		public double CurrentDepthOffset { get; set; } // Z-depth offset (simulated via scale)
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

		// Set up transform group for perspective tilt effect
		if (element.RenderTransform is not TransformGroup)
		{
			element.RenderTransform = new TransformGroup
			{
				Children =
				{
					new SkewTransform(),        // For X/Y perspective
					new ScaleTransform(),        // For depth simulation
					new TranslateTransform()     // For subtle motion
				}
			};
		}

		// Set transform origin to center for proper rotation/tilt
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

		if (!_tiltStates.TryGetValue(element, out var state) || !state.IsMouseOver)
			return;

		var mousePos = e.GetPosition(element);
		state.LastMousePosition = mousePos;

		// Calculate normalized position (-1 to 1)
		var normalizedX = (mousePos.X / element.ActualWidth) * 2 - 1;
		var normalizedY = (mousePos.Y / element.ActualHeight) * 2 - 1;

		// Calculate rotation angles based on normalized position
		var targetRotationX = normalizedY * MaxTiltAngle;      // Invert for intuitive tilt (move mouse down = tilt down)
		var targetRotationY = -normalizedX * MaxTiltAngle;
		var targetDepthScale = 1.0 - (Math.Abs(normalizedX) + Math.Abs(normalizedY)) * 0.1; // Subtle scale

		UpdateTilt(element, targetRotationX, targetRotationY, targetDepthScale);
	}

	private static void UpdateTilt(FrameworkElement element, double targetRotationX, double targetRotationY, double targetDepthScale)
	{
		if (element.RenderTransform is not TransformGroup tg || tg.Children.Count < 3)
			return;

		var skew = tg.Children[0] as SkewTransform;
		var scale = tg.Children[1] as ScaleTransform;
		var translate = tg.Children[2] as TranslateTransform;

		if (skew == null || scale == null || translate == null)
			return;

		// Animate skew values to simulate 3D tilt
		// SkewX simulates rotation around Y-axis, SkewY simulates rotation around X-axis
		AnimateTilt(skew, targetRotationY, targetRotationX, scale, targetDepthScale);
	}

	private static void AnimateTilt(SkewTransform skew, double targetSkewX, double targetSkewY, ScaleTransform scale, double targetScale)
	{
		var duration = TimeSpan.FromSeconds(AnimationDuration);
		var easing = new QuadraticEase { EasingMode = EasingMode.EaseOut };

		// Animate SkewX (simulates Y-axis rotation)
		var skewXAnimation = new DoubleAnimation
		{
			To = targetSkewX,
			Duration = duration,
			EasingFunction = easing
		};
		skew.BeginAnimation(SkewTransform.AngleXProperty, skewXAnimation);

		// Animate SkewY (simulates X-axis rotation)
		var skewYAnimation = new DoubleAnimation
		{
			To = targetSkewY,
			Duration = duration,
			EasingFunction = easing
		};
		skew.BeginAnimation(SkewTransform.AngleYProperty, skewYAnimation);

		// Animate scale (depth effect)
		var scaleAnimation = new DoubleAnimation
		{
			To = targetScale,
			Duration = duration,
			EasingFunction = easing
		};
		scale.BeginAnimation(ScaleTransform.ScaleXProperty, scaleAnimation);
		scale.BeginAnimation(ScaleTransform.ScaleYProperty, scaleAnimation);
	}

	private static void AnimateTiltToNeutral(FrameworkElement element)
	{
		if (element.RenderTransform is not TransformGroup tg || tg.Children.Count < 3)
			return;

		var skew = tg.Children[0] as SkewTransform;
		var scale = tg.Children[1] as ScaleTransform;

		if (skew == null || scale == null)
			return;

		var duration = TimeSpan.FromSeconds(AnimationDuration * 1.5);
		var easing = new QuadraticEase { EasingMode = EasingMode.EaseOut };

		// Reset SkewX
		var resetSkewXAnimation = new DoubleAnimation
		{
			To = 0,
			Duration = duration,
			EasingFunction = easing
		};
		skew.BeginAnimation(SkewTransform.AngleXProperty, resetSkewXAnimation);

		// Reset SkewY
		var resetSkewYAnimation = new DoubleAnimation
		{
			To = 0,
			Duration = duration,
			EasingFunction = easing
		};
		skew.BeginAnimation(SkewTransform.AngleYProperty, resetSkewYAnimation);

		// Reset scale
		var resetScaleAnimation = new DoubleAnimation
		{
			To = 1.0,
			Duration = duration,
			EasingFunction = easing
		};
		scale.BeginAnimation(ScaleTransform.ScaleXProperty, resetScaleAnimation);
		scale.BeginAnimation(ScaleTransform.ScaleYProperty, resetScaleAnimation);
	}
}
