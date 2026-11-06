namespace TouchKeyboard.Core;

/// <summary>
/// Selects the full-keyboard geometry.
/// </summary>
public enum FullKeyboardLayoutKind
{
    /// <summary>
    /// Modern phone keyboard interaction with the standard US ANSI key order and
    /// number row. This is the default.
    /// </summary>
    PhoneHybrid = 0,

    /// <summary>
    /// Strict physical ANSI geometry, including the Tab/Caps positions. Reserved
    /// for a future release; not implemented in v0.1.
    /// </summary>
    AnsiStrict = 1
}
