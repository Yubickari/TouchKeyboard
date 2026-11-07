namespace TouchKeyboard.Core;

/// <summary>
/// Selects the general numeric keypad surface. Profile-restricted numeric
/// keypads ignore this value and generate their keys from the input profile.
/// </summary>
public enum NumericKeypadLayoutKind
{
    /// <summary>
    /// The active 5×4 slot-based Numpad: a left function column, a 3×4 digit
    /// block, a right action column and a 1-unit Confirm. This is the default.
    /// </summary>
    Numpad = 0,

    /// <summary>
    /// The retained phone-style 5×6 numeric surface, renamed from Phone and kept
    /// for a future sequence-entry scenario. No built-in profile enables it.
    /// </summary>
    Sequence = 1
}
