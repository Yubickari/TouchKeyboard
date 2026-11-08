using System;
using Avalonia.Media;

namespace TouchKeyboard.Avalonia;

/// <summary>
/// Compact appearance facade for hosts that need a small set of knobs. It is
/// applied on top of the resolved <see cref="TouchKeyboardTheme"/> and keeps the
/// current look when every value is left at its default.
/// </summary>
/// <remarks>
/// Use <see cref="TouchKeyboardTheme"/> directly when full control over every
/// brush and metric is needed; the two approaches compose.
/// </remarks>
public sealed class TouchKeyboardStyle
{
    private const double MinScale = 0.6;
    private const double MaxScale = 1.6;

    /// <summary>
    /// Overall size multiplier for key height, fonts, icons, gaps, padding and
    /// corner radii. Default 1.0; clamped to 0.6–1.6.
    /// </summary>
    public double Scale { get; set; } = 1.0;

    /// <summary>
    /// Extra font multiplier on top of <see cref="Scale"/>; 1.0 keeps the theme
    /// sizes. Clamped to 0.6–1.6.
    /// </summary>
    public double FontScale { get; set; } = 1.0;

    /// <summary>
    /// Extra icon multiplier on top of <see cref="Scale"/>; 1.0 keeps the theme
    /// glyph sizes. Clamped to 0.6–1.6.
    /// </summary>
    public double IconScale { get; set; } = 1.0;

    /// <summary>
    /// Accent used for the confirm key, the selected candidate, the caret and the
    /// composition underline. <c>null</c> keeps the theme colours.
    /// </summary>
    public Color? AccentColor { get; set; }

    /// <summary>Overrides the confirm-key background; falls back to <see cref="AccentColor"/>.</summary>
    public Color? ConfirmColor { get; set; }

    /// <summary>Overrides the confirm-key text colour; <c>null</c> keeps the theme colour.</summary>
    public Color? ConfirmTextColor { get; set; }

    /// <summary>Overrides the selected-candidate background; falls back to <see cref="AccentColor"/>.</summary>
    public Color? SelectedCandidateColor { get; set; }

    /// <summary>Overrides the selected-candidate text colour; <c>null</c> keeps the theme colour.</summary>
    public Color? SelectedCandidateTextColor { get; set; }

    /// <summary>Overrides the keyboard panel background.</summary>
    public Color? PanelColor { get; set; }

    /// <summary>Overrides the ordinary key background.</summary>
    public Color? KeyColor { get; set; }

    /// <summary>Overrides the primary text colour (panel text and candidate text).</summary>
    public Color? TextColor { get; set; }

    internal double EffectiveScale => Clamp(Scale);

    internal double EffectiveFontScale => EffectiveScale * Clamp(FontScale);

    internal double EffectiveIconScale => Clamp(IconScale);

    internal bool IsDefault
        => Math.Abs(Scale - 1.0) < 0.0001
           && Math.Abs(FontScale - 1.0) < 0.0001
           && Math.Abs(IconScale - 1.0) < 0.0001
           && AccentColor is null
           && ConfirmColor is null
           && ConfirmTextColor is null
           && SelectedCandidateColor is null
           && SelectedCandidateTextColor is null
           && PanelColor is null
           && KeyColor is null
           && TextColor is null;

    private static double Clamp(double value)
        => Math.Clamp(value, MinScale, MaxScale);
}
