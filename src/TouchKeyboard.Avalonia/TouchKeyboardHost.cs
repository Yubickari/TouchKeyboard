using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace TouchKeyboard.Avalonia;

/// <summary>
/// Window-level host for the touch keyboard. In P1 it is a placeholder that
/// proves the Avalonia package surface and the overlay placement; the real panel,
/// editor surface and controller arrive in P3.
/// </summary>
public sealed class TouchKeyboardHost : UserControl
{
    /// <summary>
    /// Whether the host is shown.
    /// </summary>
    public static readonly StyledProperty<bool> IsOpenProperty =
        AvaloniaProperty.Register<TouchKeyboardHost, bool>(nameof(IsOpen));

    /// <summary>
    /// Creates the placeholder host.
    /// </summary>
    public TouchKeyboardHost()
    {
        Focusable = true;
        HorizontalAlignment = HorizontalAlignment.Stretch;
        VerticalAlignment = VerticalAlignment.Bottom;
        IsVisible = false;

        Content = new Border
        {
            Background = new SolidColorBrush(Color.Parse("#F7F8FA")),
            BorderBrush = new SolidColorBrush(Color.Parse("#22000000")),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(12),
            Padding = new Thickness(16),
            Margin = new Thickness(16),
            Child = new TextBlock
            {
                Text = "TouchKeyboard host — P1 skeleton",
                FontSize = 16,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            }
        };
    }

    /// <summary>
    /// Whether the host is shown. The P1 placeholder has no session or keys yet.
    /// </summary>
    public bool IsOpen
    {
        get => GetValue(IsOpenProperty);
        set => SetValue(IsOpenProperty, value);
    }

    /// <inheritdoc />
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == IsOpenProperty)
        {
            IsVisible = IsOpen;
        }
    }
}
