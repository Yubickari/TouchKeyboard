using System;
using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;

namespace TouchKeyboard.Avalonia;

/// <summary>
/// A touch-first chrome button for the overlay header. It has no hover state and
/// takes no focus; a press shows a strong overlay and a small scale-down, matching
/// <see cref="TouchKey"/> on a touch panel.
/// </summary>
internal sealed class TouchButton : Border
{
    private readonly Grid _root;
    private readonly Border _overlay;
    private readonly ScaleTransform _pressTransform = new ScaleTransform(1, 1);
    private Control? _content;
    private IBrush _pressedOverlay = Brushes.Transparent;
    private double _pressedScale = 0.96;
    private bool _pressed;

    /// <summary>Creates an empty chrome button.</summary>
    public TouchButton()
    {
        Focusable = false;
        ClipToBounds = true;

        _overlay = new Border
        {
            IsHitTestVisible = false,
            IsVisible = false,
            Background = Brushes.Transparent
        };

        _root = new Grid();
        _root.Children.Add(_overlay);
        Child = _root;

        RenderTransformOrigin = RelativePoint.Center;
        RenderTransform = _pressTransform;
    }

    /// <summary>Raised when the button is released after a press.</summary>
    public event EventHandler? Activated;

    /// <summary>Brush painted over the whole button while it is pressed.</summary>
    public IBrush PressedOverlay
    {
        get => _pressedOverlay;
        set
        {
            _pressedOverlay = value ?? Brushes.Transparent;
            _overlay.Background = _pressedOverlay;
        }
    }

    /// <summary>Scale applied to the button while it is pressed.</summary>
    public double PressedScale
    {
        get => _pressedScale;
        set => _pressedScale = value > 0 && !double.IsNaN(value) ? value : 1;
    }

    /// <summary>Corner radius of the button and of its pressed overlay.</summary>
    public double Radius
    {
        get => CornerRadius.TopLeft;
        set
        {
            var radius = new CornerRadius(Math.Max(0, value));
            CornerRadius = radius;
            _overlay.CornerRadius = radius;
        }
    }

    /// <summary>Replaces the visual content.</summary>
    public void SetContent(Control content)
    {
        if (content is null)
        {
            throw new ArgumentNullException(nameof(content));
        }

        if (_content is not null)
        {
            _root.Children.Remove(_content);
        }

        _content = content;
        _root.Children.Insert(0, content);
    }

    /// <inheritdoc />
    protected override AutomationPeer OnCreateAutomationPeer()
        => new KeyboardAutomationPeer(this, nameof(TouchButton));

    /// <inheritdoc />
    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);

        if (!IsEnabled)
        {
            return;
        }

        e.Handled = true;
        _pressed = true;
        _overlay.IsVisible = true;
        _pressTransform.ScaleX = _pressedScale;
        _pressTransform.ScaleY = _pressedScale;
        e.Pointer.Capture(this);
    }

    /// <inheritdoc />
    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);

        if (!_pressed)
        {
            return;
        }

        e.Handled = true;
        ResetPress();
        e.Pointer.Capture(null);
        Activated?.Invoke(this, EventArgs.Empty);
    }

    /// <inheritdoc />
    protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
    {
        base.OnPointerCaptureLost(e);
        ResetPress();
    }

    private void ResetPress()
    {
        _pressed = false;
        _overlay.IsVisible = false;
        _pressTransform.ScaleX = 1;
        _pressTransform.ScaleY = 1;
    }
}
