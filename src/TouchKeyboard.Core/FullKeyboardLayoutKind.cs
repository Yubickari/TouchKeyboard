namespace TouchKeyboard.Core;

/// <summary>
/// Selects the full-keyboard geometry.
/// </summary>
public enum FullKeyboardLayoutKind
{
    /// <summary>
    /// Four-row phone-style QWERTY: letters only on the main page, with symbol and
    /// numeric entry points and no number row. A retained alternative to
    /// <see cref="AnsiStyle"/>.
    /// </summary>
    PhoneHybrid = 0,

    /// <summary>
    /// Strict 15-unit ANSI geometry: five rows with Tab/Caps/IME reserved spaces,
    /// exact Enter/Shift/backslash widths and a bottom function row. The default.
    /// </summary>
    AnsiStyle = 1
}
