using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using TouchKeyboard.Core;

namespace TouchKeyboard.Avalonia;

/// <summary>
/// A touch key. It deliberately does not inherit Button, does not take focus and
/// captures the pointer so a small finger movement does not lose the press.
/// </summary>
public sealed class TouchKey : Border
{
    private readonly TextBlock _primary;
    private readonly TextBlock _secondary;
    private readonly DispatcherTimer _repeatTimer;
    private readonly ITouchKeyboardLocalizer _localizer;
    private KeyDefinition _definition;
    private TouchKeyboardTheme _theme;
    private bool _shiftActive;
    private bool _pressed;

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
        Padding = new Thickness(4, 2);

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

        var grid = new Grid();
        grid.Children.Add(_primary);
        grid.Children.Add(_secondary);
        Child = grid;

        _repeatTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(420) };
        _repeatTimer.Tick += OnRepeatTick;

        ApplyTheme();
        UpdateLabels();
    }

    /// <summary>The key definition this view renders.</summary>
    public KeyDefinition Definition => _definition;

    /// <summary>Raised when the key should execute its command.</summary>
    public event EventHandler? Activated;

    /// <summary>Updates the label when sticky shift changes.</summary>
    public void SetShift(bool active)
    {
        if (_shiftActive == active)
        {
            return;
        }

        _shiftActive = active;
        UpdateLabels();
    }

    /// <summary>Reapplies a theme after the host changes it.</summary>
    public void SetTheme(TouchKeyboardTheme theme)
    {
        _theme = theme ?? throw new ArgumentNullException(nameof(theme));
        ApplyTheme();
        UpdateLabels();
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
        Opacity = _theme.PressedOpacity;
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
        _pressed = false;
        Opacity = 1;
        StopRepeat();
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
        Opacity = 1;
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
        _primary.FontSize = IsActionStyle ? _theme.ActionFontSize : _theme.KeyFontSize;
        _secondary.FontSize = _theme.ShiftedFontSize;

        switch (_definition.Style)
        {
            case KeyStyle.Primary:
                Background = _theme.PrimaryKeyBackground;
                BorderBrush = null;
                BorderThickness = default;
                _primary.Foreground = _theme.PrimaryKeyForeground;
                _secondary.Foreground = _theme.PrimaryKeyForeground;
                break;
            case KeyStyle.Action:
                Background = _theme.ActionKeyBackground;
                BorderBrush = null;
                BorderThickness = default;
                _primary.Foreground = _theme.Text;
                _secondary.Foreground = _theme.MutedText;
                break;
            case KeyStyle.Danger:
                Background = _theme.DangerKeyBackground;
                BorderBrush = null;
                BorderThickness = default;
                _primary.Foreground = Brushes.White;
                _secondary.Foreground = Brushes.White;
                break;
            default:
                Background = _theme.KeyBackground;
                BorderBrush = _theme.KeyBorder;
                BorderThickness = new Thickness(1);
                _primary.Foreground = _theme.Text;
                _secondary.Foreground = _theme.MutedText;
                break;
        }
    }

    private void UpdateLabels()
    {
        var label = Resolve(_definition.Label);
        var shifted = _definition.ShiftedLabel is { } shiftedLabel ? Resolve(shiftedLabel) : string.Empty;

        var primary = _shiftActive && shifted.Length > 0 ? shifted : label;
        var secondary = _shiftActive ? label : shifted;

        _primary.Text = primary;
        _secondary.Text = secondary;
        _secondary.IsVisible = secondary.Length > 0 && secondary != primary;
    }

    private string Resolve(KeyLabel label) => label.IsResource ? _localizer.Get(label.ResourceKey!) : label.Text ?? string.Empty;

    private bool IsActionStyle
        => _definition.Style is KeyStyle.Action or KeyStyle.Primary or KeyStyle.Danger;
}
