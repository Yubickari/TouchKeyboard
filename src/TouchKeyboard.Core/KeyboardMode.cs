namespace TouchKeyboard.Core;

/// <summary>
/// The two top-level keyboard surfaces.
/// </summary>
public enum KeyboardMode
{
    /// <summary>
    /// Standard US ANSI key order with a number row, rendered with the modern
    /// phone keyboard interaction model.
    /// </summary>
    FullKeyboard = 0,

    /// <summary>
    /// A numeric surface: either the general numeric keypad or a profile-restricted
    /// keypad that only exposes the keys the field accepts.
    /// </summary>
    NumericKeypad = 1
}
