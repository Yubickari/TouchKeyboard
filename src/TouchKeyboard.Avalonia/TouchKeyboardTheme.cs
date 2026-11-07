using Avalonia.Media;

namespace TouchKeyboard.Avalonia;

/// <summary>
/// Brushes and metrics for the keyboard chrome and keys. Frontends may derive
/// their own instance; the host rebuilds key views when the theme changes.
/// </summary>
public sealed class TouchKeyboardTheme
{
    /// <summary>Default light theme.</summary>
    public static TouchKeyboardTheme Light { get; } = new TouchKeyboardTheme();

    /// <summary>Dark theme for low-light panels.</summary>
    public static TouchKeyboardTheme Dark { get; } = new TouchKeyboardTheme
    {
        Scrim = Brush("#88000000"),
        PanelBackground = Brush("#FF1F2937"),
        KeyBackground = Brush("#FF374151"),
        KeyBorder = Brush("#33FFFFFF"),
        ActionKeyBackground = Brush("#FF4B5563"),
        PrimaryKeyBackground = Brush("#FF3B82F6"),
        PrimaryKeyForeground = Brush("#FFFFFFFF"),
        DangerKeyBackground = Brush("#FFDC2626"),
        DangerKeyForeground = Brush("#FFFFFFFF"),
        Text = Brush("#FFF9FAFB"),
        MutedText = Brush("#FF9CA3AF"),
        ErrorText = Brush("#FFF87171"),
        EditorBackground = Brush("#FF111827"),
        EditorBorder = Brush("#33FFFFFF"),
        Caret = Brush("#FF60A5FA"),
        PressedOverlay = Brush("#33FFFFFF")
    };

    /// <summary>Scrim behind the panel.</summary>
    public IBrush Scrim { get; init; } = Brush("#66000000");

    /// <summary>Panel background.</summary>
    public IBrush PanelBackground { get; init; } = Brush("#FFF7F8FA");

    /// <summary>Ordinary key background.</summary>
    public IBrush KeyBackground { get; init; } = Brush("#FFFFFFFF");

    /// <summary>Ordinary key border.</summary>
    public IBrush KeyBorder { get; init; } = Brush("#14000000");

    /// <summary>Function key background.</summary>
    public IBrush ActionKeyBackground { get; init; } = Brush("#FFE5E8EF");

    /// <summary>Confirm key background.</summary>
    public IBrush PrimaryKeyBackground { get; init; } = Brush("#FF2563EB");

    /// <summary>Confirm key foreground.</summary>
    public IBrush PrimaryKeyForeground { get; init; } = Brush("#FFFFFFFF");

    /// <summary>Close key background.</summary>
    public IBrush DangerKeyBackground { get; init; } = Brush("#FFDC2626");

    /// <summary>Close key foreground; white reads on the red in both themes.</summary>
    public IBrush DangerKeyForeground { get; init; } = Brush("#FFFFFFFF");

    /// <summary>Primary text.</summary>
    public IBrush Text { get; init; } = Brush("#FF1F2937");

    /// <summary>Muted text.</summary>
    public IBrush MutedText { get; init; } = Brush("#FF6B7280");

    /// <summary>Validation error text.</summary>
    public IBrush ErrorText { get; init; } = Brush("#FFDC2626");

    /// <summary>Editor background.</summary>
    public IBrush EditorBackground { get; init; } = Brush("#FFFFFFFF");

    /// <summary>Editor border.</summary>
    public IBrush EditorBorder { get; init; } = Brush("#22000000");

    /// <summary>Caret brush.</summary>
    public IBrush Caret { get; init; } = Brush("#FF2563EB");

    /// <summary>Primary key font size.</summary>
    public double KeyFontSize { get; init; } = 26;

    /// <summary>Function/confirm key font size.</summary>
    public double ActionFontSize { get; init; } = 20;

    /// <summary>Shifted-label font size.</summary>
    public double ShiftedFontSize { get; init; } = 12;

    /// <summary>Key corner radius.</summary>
    public double KeyCornerRadius { get; init; } = 10;

    /// <summary>Panel corner radius.</summary>
    public double PanelCornerRadius { get; init; } = 18;

    /// <summary>Gap between keys.</summary>
    public double KeyGap { get; init; } = 6;

    /// <summary>Panel inner padding.</summary>
    public double PanelPadding { get; init; } = 16;

    /// <summary>Editor font size.</summary>
    public double EditorFontSize { get; init; } = 28;

    /// <summary>Title font size.</summary>
    public double TitleFontSize { get; init; } = 22;

    /// <summary>Unit font size.</summary>
    public double UnitFontSize { get; init; } = 14;

    /// <summary>
    /// Brush painted over a key or chrome button while it is pressed. A dark
    /// overlay on the light theme and a light overlay on the dark theme keeps the
    /// press visible without touching the label colour.
    /// </summary>
    public IBrush PressedOverlay { get; init; } = Brush("#2E000000");

    /// <summary>Scale applied to a key while it is pressed.</summary>
    public double PressedScale { get; init; } = 0.96;

    /// <summary>
    /// Downward optical offset of key labels in pixels at <see cref="KeyFontSize"/>.
    /// Digits and capitals otherwise sit slightly above the visual centre.
    /// </summary>
    public double KeyLabelOffset { get; init; } = 2;

    private static SolidColorBrush Brush(string hex) => new SolidColorBrush(Color.Parse(hex));
}
