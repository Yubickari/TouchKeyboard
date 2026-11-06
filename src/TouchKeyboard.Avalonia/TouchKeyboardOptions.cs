using System;
using TouchKeyboard.Core;

namespace TouchKeyboard.Avalonia;

/// <summary>
/// Host configuration. The defaults target a 1280x800 landscape panel.
/// </summary>
public sealed class TouchKeyboardOptions
{
    /// <summary>Global switch; when false the controller does not open the panel.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Visual theme.</summary>
    public TouchKeyboardTheme Theme { get; set; } = TouchKeyboardTheme.Light;

    /// <summary>Full-keyboard geometry.</summary>
    public FullKeyboardLayoutKind FullKeyboardLayout { get; set; } = FullKeyboardLayoutKind.PhoneHybrid;

    /// <summary>Default general numeric surface.</summary>
    public NumericKeypadLayoutKind NumericKeypadLayout { get; set; } = NumericKeypadLayoutKind.Numpad;

    /// <summary>Maximum panel width in logical pixels.</summary>
    public double MaxPanelWidth { get; set; } = 1280;

    /// <summary>Maximum width of the numeric keypad content.</summary>
    public double NumericPanelMaxWidth { get; set; } = 720;

    /// <summary>Fixed key height; 0 selects 72/64/56 from the window height.</summary>
    public double KeyHeight { get; set; }

    /// <summary>Whether the host handles physical keyboard input while open.</summary>
    public bool HandlePhysicalKeyboard { get; set; } = true;

    /// <summary>
    /// Reserved for platform-specific suppression. v1 suppresses the system OSK
    /// by moving focus to the host rather than by platform hooks.
    /// </summary>
    public bool SuppressSystemKeyboard { get; set; } = true;

    /// <summary>Culture name used by the built-in localizer; null follows the current UI culture.</summary>
    public string? Culture { get; set; }

    /// <summary>Optional localizer override.</summary>
    public ITouchKeyboardLocalizer? Localizer { get; set; }

    /// <summary>Optional editor surface factory; the default is the custom-drawn <see cref="TouchKeyboardEditor"/>.</summary>
    public Func<IEditorSurface>? EditorSurfaceFactory { get; set; }
}
