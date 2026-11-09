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
        CandidateBackground = Brush("#FF374151"),
        CandidateSelectedBackground = Brush("#FF1D4ED8"),
        CandidateText = Brush("#FFF9FAFB"),
        CandidateSelectedText = Brush("#FFFFFFFF"),
        CandidateMutedText = Brush("#FF9CA3AF"),
        CompositionText = Brush("#FF93C5FD"),
        CompositionUnderline = Brush("#FF60A5FA"),
        PressedOverlay = Brush("#33FFFFFF")
    };

    /// <summary>
    /// Font family used by key labels, header chrome, the editor and candidates.
    /// Defaults to the platform-aware <see cref="TouchKeyboardFonts.Default"/>.
    /// </summary>
    public FontFamily FontFamily { get; init; } = TouchKeyboardFonts.Default;

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

    /// <summary>Candidate button background.</summary>
    public IBrush CandidateBackground { get; init; } = Brush("#FFF0F2F7");

    /// <summary>Background of the default/selected candidate.</summary>
    public IBrush CandidateSelectedBackground { get; init; } = Brush("#FFDBEAFE");

    /// <summary>Candidate text.</summary>
    public IBrush CandidateText { get; init; } = Brush("#FF1F2937");

    /// <summary>Text of the default/selected candidate.</summary>
    public IBrush CandidateSelectedText { get; init; } = Brush("#FF1E3A8A");

    /// <summary>Muted candidate-bar text: page number and hints.</summary>
    public IBrush CandidateMutedText { get; init; } = Brush("#FF6B7280");

    /// <summary>Inline composition text in the editor.</summary>
    public IBrush CompositionText { get; init; } = Brush("#FF2563EB");

    /// <summary>Underline under the inline composition text.</summary>
    public IBrush CompositionUnderline { get; init; } = Brush("#FF2563EB");

    /// <summary>Primary key font size.</summary>
    public double KeyFontSize { get; init; } = 26;

    /// <summary>Function/confirm key font size.</summary>
    public double ActionFontSize { get; init; } = 20;

    /// <summary>Shifted-label font size.</summary>
    public double ShiftedFontSize { get; init; } = 12;

    /// <summary>Extra multiplier applied to key glyph icons; 1.0 keeps the default factor.</summary>
    public double IconScale { get; init; } = 1.0;

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

    /// <summary>
    /// Candidate row height in logical pixels on AnsiStyle. PhoneHybrid uses the
    /// key row height when the candidate row is the first of its five rows.
    /// </summary>
    public double CandidateBarHeight { get; init; } = 44;

    /// <summary>Candidate button corner radius.</summary>
    public double CandidateCornerRadius { get; init; } = 8;

    /// <summary>Candidate font size.</summary>
    public double CandidateFontSize { get; init; } = 18;

    /// <summary>Thickness of the inline composition underline.</summary>
    public double CompositionUnderlineThickness { get; init; } = 1.5;

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

    /// <summary>
    /// Applies the compact <see cref="TouchKeyboardStyle"/> facade on top of this
    /// theme. Returns the same instance when the style is at its defaults so the
    /// built-in presets keep reference identity.
    /// </summary>
    internal TouchKeyboardTheme ApplyStyle(TouchKeyboardStyle style)
    {
        if (style is null || style.IsDefault)
        {
            return this;
        }

        var scale = style.EffectiveScale;
        var font = style.EffectiveFontScale;
        var accent = style.AccentColor is { } accentColor ? (IBrush)new SolidColorBrush(accentColor) : null;
        var confirm = style.ConfirmColor is { } confirmColor ? new SolidColorBrush(confirmColor) : accent;
        var selected = style.SelectedCandidateColor is { } selectedColor
            ? new SolidColorBrush(selectedColor)
            : accent;

        return new TouchKeyboardTheme
        {
            FontFamily = style.FontFamily ?? FontFamily,
            Scrim = Scrim,
            PanelBackground = style.PanelColor is { } panel ? new SolidColorBrush(panel) : PanelBackground,
            KeyBackground = style.KeyColor is { } key ? new SolidColorBrush(key) : KeyBackground,
            KeyBorder = KeyBorder,
            ActionKeyBackground = ActionKeyBackground,
            PrimaryKeyBackground = confirm ?? PrimaryKeyBackground,
            PrimaryKeyForeground = style.ConfirmTextColor is { } confirmText
                ? new SolidColorBrush(confirmText)
                : PrimaryKeyForeground,
            DangerKeyBackground = DangerKeyBackground,
            DangerKeyForeground = DangerKeyForeground,
            Text = style.TextColor is { } text ? new SolidColorBrush(text) : Text,
            MutedText = MutedText,
            ErrorText = ErrorText,
            EditorBackground = EditorBackground,
            EditorBorder = EditorBorder,
            Caret = accent ?? Caret,
            CandidateBackground = CandidateBackground,
            CandidateSelectedBackground = selected ?? CandidateSelectedBackground,
            CandidateText = style.TextColor is { } candidateText ? new SolidColorBrush(candidateText) : CandidateText,
            CandidateSelectedText = style.SelectedCandidateTextColor is { } selectedText
                ? new SolidColorBrush(selectedText)
                : CandidateSelectedText,
            CandidateMutedText = CandidateMutedText,
            CompositionText = accent ?? CompositionText,
            CompositionUnderline = accent ?? CompositionUnderline,
            KeyFontSize = KeyFontSize * font,
            ActionFontSize = ActionFontSize * font,
            ShiftedFontSize = ShiftedFontSize * font,
            IconScale = IconScale * style.EffectiveIconScale,
            KeyCornerRadius = KeyCornerRadius * scale,
            PanelCornerRadius = PanelCornerRadius * scale,
            KeyGap = KeyGap * scale,
            PanelPadding = PanelPadding * scale,
            EditorFontSize = EditorFontSize * font,
            CandidateBarHeight = CandidateBarHeight * scale,
            CandidateCornerRadius = CandidateCornerRadius * scale,
            CandidateFontSize = CandidateFontSize * font,
            CompositionUnderlineThickness = CompositionUnderlineThickness * scale,
            TitleFontSize = TitleFontSize * font,
            UnitFontSize = UnitFontSize * font,
            PressedOverlay = PressedOverlay,
            PressedScale = PressedScale,
            KeyLabelOffset = KeyLabelOffset * font
        };
    }

    private static SolidColorBrush Brush(string hex) => new SolidColorBrush(Color.Parse(hex));
}
