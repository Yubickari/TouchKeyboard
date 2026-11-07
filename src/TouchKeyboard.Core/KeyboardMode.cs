namespace TouchKeyboard.Core;

/// <summary>
/// The two top-level keyboard surfaces.
/// </summary>
public enum KeyboardMode
{
    /// <summary>
    /// The full keyboard: the strict 15-unit ANSI geometry or the four-row
    /// phone-style letters keyboard, both without F, numpad or system keys.
    /// </summary>
    FullKeyboard = 0,

    /// <summary>
    /// A numeric surface: either the general numeric keypad or a profile-restricted
    /// keypad that only exposes the keys the field accepts.
    /// </summary>
    NumericKeypad = 1
}
