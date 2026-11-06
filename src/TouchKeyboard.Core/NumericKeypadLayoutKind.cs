namespace TouchKeyboard.Core;

/// <summary>
/// Selects the general numeric keypad surface. Profile-restricted numeric
/// keypads ignore this value and generate their keys from the input profile.
/// </summary>
public enum NumericKeypadLayoutKind
{
    /// <summary>
    /// A 6x4 numeric block with common symbols, space and a full-keyboard switch.
    /// This is the default.
    /// </summary>
    Numpad = 0,

    /// <summary>
    /// A phone-style 123 page: digits plus common English punctuation, with a
    /// full-keyboard switch, space, backspace and confirm.
    /// </summary>
    Phone = 1
}
