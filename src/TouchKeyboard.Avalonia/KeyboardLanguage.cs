namespace TouchKeyboard.Avalonia;

/// <summary>
/// Built-in language used for the keyboard chrome and key legends. It does not
/// change the letter geometry: both languages share the same US ANSI QWERTY
/// surfaces, and only named key labels, validation messages and chrome strings
/// are localized.
/// </summary>
/// <remarks>
/// The optional IME input mode (Chinese or English composition) is separate and
/// is controlled by <see cref="TouchKeyboardImeOptions"/>.
/// </remarks>
public enum KeyboardLanguage
{
    /// <summary>
    /// Follow <see cref="TouchKeyboardOptions.Culture"/> when it is set, otherwise
    /// follow the current UI culture.
    /// </summary>
    Auto,

    /// <summary>Simplified Chinese labels and validation messages (zh-CN).</summary>
    Chinese,

    /// <summary>English labels and validation messages (en-US).</summary>
    English
}
