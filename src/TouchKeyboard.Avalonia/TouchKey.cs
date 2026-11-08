using System;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using TouchKeyboard.Core;
using AvaloniaPath = Avalonia.Controls.Shapes.Path;

namespace TouchKeyboard.Avalonia;

/// <summary>
/// A touch key. It deliberately does not inherit Button, does not take focus and
/// captures the pointer so a small finger movement does not lose the press.
/// Function keys with a <see cref="KeyGlyph"/> are drawn as vectors; the localized
/// label stays the accessible name and the text fallback. A press paints a strong
/// overlay and scales the cap down, since touch panels have no hover feedback.
/// </summary>
public sealed class TouchKey : Border
{
    private readonly TextBlock _primary;
    private readonly TextBlock _secondary;
    private readonly AvaloniaPath _glyphPath;
    private readonly Viewbox _glyphHost;
    private readonly Border _pressOverlay;
    private readonly ScaleTransform _pressTransform = new ScaleTransform(1, 1);
    private readonly DispatcherTimer _repeatTimer;
    private readonly ITouchKeyboardLocalizer _localizer;
    private KeyDefinition _definition;
    private TouchKeyboardTheme _theme;
    private bool _shiftActive;
    private bool _pressed;
    private double _iconSize = 24;
    private IBrush _foregroundBrush = Brushes.Black;

    /// <summary>Creates a key for one definition.</summary>
    public TouchKey(
        KeyDefinition definition,
        TouchKeyboardTheme theme,
        ITouchKeyboardLocalizer localizer,
        bool shiftActive)
    {
        _definition = definition ?? throw new ArgumentNullException(nameof(definition));
        _theme = theme ?? throw new ArgumentNullException(nameof(theme));
        _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
        _shiftActive = shiftActive;

        Focusable = false;
        ClipToBounds = true;
        HorizontalAlignment = HorizontalAlignment.Stretch;
        VerticalAlignment = VerticalAlignment.Stretch;

        _primary = new TextBlock
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            TextAlignment = TextAlignment.Center,
            TextTrimming = TextTrimming.CharacterEllipsis,
            TextWrapping = TextWrapping.NoWrap
        };
        _secondary = new TextBlock
        {
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Top,
            TextAlignment = TextAlignment.Right,
            Margin = new Thickness(0, 2, 4, 0)
        };
        _glyphPath = new AvaloniaPath
        {
            Stretch = Stretch.None,
            Width = TouchKeyboardGlyphs.DesignSize,
            Height = TouchKeyboardGlyphs.DesignSize,
            StrokeLineCap = PenLineCap.Round,
            StrokeJoin = PenLineJoin.Round
        };
        _glyphHost = new Viewbox
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Stretch = Stretch.Uniform,
            Child = _glyphPath,
            IsVisible = false
        };
        _pressOverlay = new Border
        {
            IsHitTestVisible = false,
            IsVisible = false
        };

        var content = new Grid
        {
            Margin = new Thickness(4, 2)
        };
        content.Children.Add(_glyphHost);
        content.Children.Add(_primary);
        content.Children.Add(_secondary);

        // The press overlay sits outside the content margin so it fills the whole cap.
        var root = new Grid();
        root.Children.Add(content);
        root.Children.Add(_pressOverlay);
        Child = root;

        RenderTransformOrigin = RelativePoint.Center;
        RenderTransform = _pressTransform;

        _repeatTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(420) };
        _repeatTimer.Tick += OnRepeatTick;

        ApplyTheme();
    }

    /// <summary>The key definition this view renders.</summary>
    public KeyDefinition Definition => _definition;

    /// <summary>Raised when the key should execute its command.</summary>
    public event EventHandler? Activated;

    /// <summary>
    /// Reference icon size used to scale the shared glyph canvas; the frontend
    /// derives it from the key height.
    /// </summary>
    public double IconSize
    {
        get => _iconSize;
        set
        {
            var size = value <= 0 || double.IsNaN(value) ? 24 : value;
            if (Math.Abs(_iconSize - size) < 0.01)
            {
                return;
            }

            _iconSize = size;
            UpdateContent();
        }
    }

    /// <summary>Updates the label when sticky shift changes.</summary>
    public void SetShift(bool active)
    {
        if (_shiftActive == active)
        {
            return;
        }

        _shiftActive = active;
        UpdateContent();
    }

    /// <summary>Reapplies a theme after the host changes it.</summary>
    public void SetTheme(TouchKeyboardTheme theme)
    {
        _theme = theme ?? throw new ArgumentNullException(nameof(theme));
        ApplyTheme();
    }

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
        _pressOverlay.IsVisible = true;
        _pressTransform.ScaleX = _theme.PressedScale;
        _pressTransform.ScaleY = _theme.PressedScale;
        e.Pointer.Capture(this);

        if (_definition.Repeatable)
        {
            Activate();
            StartRepeat();
        }
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
        CancelPress();
        e.Pointer.Capture(null);

        if (!_definition.Repeatable)
        {
            Activate();
        }
    }

    /// <inheritdoc />
    protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
    {
        base.OnPointerCaptureLost(e);
        CancelPress();
    }

    /// <inheritdoc />
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        StopRepeat();
    }

    /// <summary>Stops a long-press repeat, for example when the panel closes.</summary>
    public void CancelRepeat() => StopRepeat();

    /// <inheritdoc />
    protected override AutomationPeer OnCreateAutomationPeer()
        => new KeyboardAutomationPeer(this, nameof(TouchKey));

    private void OnRepeatTick(object? sender, EventArgs e)
    {
        if (!_pressed || !IsEnabled)
        {
            StopRepeat();
            return;
        }

        _repeatTimer.Interval = TimeSpan.FromMilliseconds(55);
        Activate();
    }

    private void StartRepeat()
    {
        StopRepeat();
        _repeatTimer.Interval = TimeSpan.FromMilliseconds(420);
        _repeatTimer.Start();
    }

    private void StopRepeat() => _repeatTimer.Stop();

    private void CancelPress()
    {
        _pressed = false;
        _pressOverlay.IsVisible = false;
        _pressTransform.ScaleX = 1;
        _pressTransform.ScaleY = 1;
        StopRepeat();
    }

    private void Activate()
    {
        if (IsEnabled)
        {
            Activated?.Invoke(this, EventArgs.Empty);
        }
    }

    private void ApplyTheme()
    {
        CornerRadius = new CornerRadius(_theme.KeyCornerRadius);
        _pressOverlay.CornerRadius = new CornerRadius(_theme.KeyCornerRadius);
        _pressOverlay.Background = _theme.PressedOverlay;

        _primary.FontSize = IsActionStyle ? _theme.ActionFontSize : _theme.KeyFontSize;
        _secondary.FontSize = _theme.ShiftedFontSize;

        // Digits and capitals sit slightly above the line box centre; nudge the
        // label down by a themed optical offset.
        var referenceSize = _theme.KeyFontSize > 0 ? _theme.KeyFontSize : _primary.FontSize;
        var labelOffset = referenceSize > 0 ? _theme.KeyLabelOffset * _primary.FontSize / referenceSize : 0;
        _primary.RenderTransform = new TranslateTransform(0, labelOffset);

        switch (_definition.Style)
        {
            case KeyStyle.Primary:
                Background = _theme.PrimaryKeyBackground;
                BorderBrush = null;
                BorderThickness = default;
                _foregroundBrush = _theme.PrimaryKeyForeground;
                _secondary.Foreground = _theme.PrimaryKeyForeground;
                break;
            case KeyStyle.Action:
                Background = _theme.ActionKeyBackground;
                BorderBrush = null;
                BorderThickness = default;
                _foregroundBrush = _theme.Text;
                _secondary.Foreground = _theme.MutedText;
                break;
            case KeyStyle.Danger:
                Background = _theme.DangerKeyBackground;
                BorderBrush = null;
                BorderThickness = default;
                _foregroundBrush = _theme.DangerKeyForeground;
                _secondary.Foreground = _theme.DangerKeyForeground;
                break;
            default:
                Background = _theme.KeyBackground;
                BorderBrush = _theme.KeyBorder;
                BorderThickness = new Thickness(1);
                _foregroundBrush = _theme.Text;
                _secondary.Foreground = _theme.MutedText;
                break;
        }

        _primary.Foreground = _foregroundBrush;
        UpdateContent();
    }

    private void UpdateContent()
    {
        var label = Resolve(_definition.Label);
        var shifted = _definition.ShiftedLabel is { } shiftedLabel ? Resolve(shiftedLabel) : string.Empty;

        var primary = _shiftActive && shifted.Length > 0 ? shifted : label;
        var secondary = _shiftActive ? label : shifted;

        var glyph = ResolveGlyph();
        var showGlyph = glyph.HasValue;

        _primary.Text = primary;
        _primary.IsVisible = !showGlyph;
        _secondary.Text = secondary;
        _secondary.IsVisible = !showGlyph && secondary.Length > 0 && secondary != primary;

        // Text keys can override the short cap label with a full sentence (the IME
        // toggle); other keys keep their localized label as the accessible name.
        var accessibleName = _definition.AccessibleName is { } custom ? Resolve(custom) : primary;
        AutomationProperties.SetName(this, accessibleName);

        if (glyph is { } key && TouchKeyboardGlyphs.TryGet(key, out var shape))
        {
            TouchKeyboardGlyphs.Apply(_glyphPath, key, _foregroundBrush);
            var box = _iconSize * shape.SizeFactor;
            _glyphHost.Width = box;
            _glyphHost.Height = box;
            _glyphHost.IsVisible = true;
            _primary.IsVisible = false;
        }
        else
        {
            _glyphHost.IsVisible = false;
        }
    }

    private KeyGlyph? ResolveGlyph()
    {
        if (_definition.Glyph is not { } glyph)
        {
            return null;
        }

        // Shift switches from the outline to the filled state while it is active.
        if (glyph == KeyGlyph.Shift && _shiftActive)
        {
            return KeyGlyph.ShiftFilled;
        }

        return glyph;
    }

    private string Resolve(KeyLabel label) => label.IsResource ? _localizer.Get(label.ResourceKey!) : label.Text ?? string.Empty;

    private bool IsActionStyle
        => _definition.Style is KeyStyle.Action or KeyStyle.Primary or KeyStyle.Danger;
}
