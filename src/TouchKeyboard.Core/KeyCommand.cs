using System;

namespace TouchKeyboard.Core;

/// <summary>
/// The closed set of UI-agnostic actions a key can dispatch to a session.
/// </summary>
public enum KeyCommandKind
{
    /// <summary>No action; used by spacers.</summary>
    None = 0,

    /// <summary>Insert the attached text at the caret.</summary>
    InsertText,

    /// <summary>Delete the character before the caret.</summary>
    Backspace,

    /// <summary>Clear the whole buffer.</summary>
    Clear,

    /// <summary>Move the caret one character left.</summary>
    MoveCaretLeft,

    /// <summary>Move the caret one character right.</summary>
    MoveCaretRight,

    /// <summary>Increment the digit immediately left of the caret, wrapping 9 to 0.</summary>
    StepDigitUp,

    /// <summary>Decrement the digit immediately left of the caret, wrapping 0 to 9.</summary>
    StepDigitDown,

    /// <summary>Toggle a leading minus sign.</summary>
    ToggleSign,

    /// <summary>Toggle the sticky shift state in the full keyboard.</summary>
    ToggleShift,

    /// <summary>Switch to the full keyboard, when the profile allows it.</summary>
    SwitchToFullKeyboard,

    /// <summary>Switch to the general numeric keypad, when the profile allows it.</summary>
    SwitchToNumericKeypad,

    /// <summary>Open the full-keyboard symbol page, when the geometry has one.</summary>
    SwitchToSymbolPage,

    /// <summary>Return from the symbol page to the full-keyboard main page.</summary>
    SwitchToMainPage,

    /// <summary>Validate and submit; the host performs the write-back.</summary>
    Submit,

    /// <summary>Close and discard; the host performs the close.</summary>
    Close,

    /// <summary>Toggle the IME between Chinese and English input; only when an engine is configured.</summary>
    ToggleIme
}

/// <summary>
/// A UI-agnostic key action. Character keys carry their text; all other keys use
/// only the kind.
/// </summary>
public readonly struct KeyCommand : IEquatable<KeyCommand>
{
    /// <summary>Creates a command.</summary>
    public KeyCommand(KeyCommandKind kind, string? text = null)
    {
        Kind = kind;
        Text = text;
    }

    /// <summary>The action kind.</summary>
    public KeyCommandKind Kind { get; }

    /// <summary>The inserted text, for <see cref="KeyCommandKind.InsertText"/>.</summary>
    public string? Text { get; }

    /// <summary>No action.</summary>
    public static KeyCommand None => new KeyCommand(KeyCommandKind.None);

    /// <summary>Insert text at the caret.</summary>
    public static KeyCommand Insert(string text)
    {
        if (text is null)
        {
            throw new ArgumentNullException(nameof(text));
        }

        return new KeyCommand(KeyCommandKind.InsertText, text);
    }

    /// <summary>Delete one character before the caret.</summary>
    public static KeyCommand Backspace => new KeyCommand(KeyCommandKind.Backspace);

    /// <summary>Clear the buffer.</summary>
    public static KeyCommand Clear => new KeyCommand(KeyCommandKind.Clear);

    /// <summary>Move the caret left.</summary>
    public static KeyCommand MoveCaretLeft => new KeyCommand(KeyCommandKind.MoveCaretLeft);

    /// <summary>Move the caret right.</summary>
    public static KeyCommand MoveCaretRight => new KeyCommand(KeyCommandKind.MoveCaretRight);

    /// <summary>Increment the digit left of the caret.</summary>
    public static KeyCommand StepDigitUp => new KeyCommand(KeyCommandKind.StepDigitUp);

    /// <summary>Decrement the digit left of the caret.</summary>
    public static KeyCommand StepDigitDown => new KeyCommand(KeyCommandKind.StepDigitDown);

    /// <summary>Toggle a leading minus sign.</summary>
    public static KeyCommand ToggleSign => new KeyCommand(KeyCommandKind.ToggleSign);

    /// <summary>Toggle shift.</summary>
    public static KeyCommand ToggleShift => new KeyCommand(KeyCommandKind.ToggleShift);

    /// <summary>Switch to the full keyboard.</summary>
    public static KeyCommand SwitchToFullKeyboard => new KeyCommand(KeyCommandKind.SwitchToFullKeyboard);

    /// <summary>Switch to the general numeric keypad.</summary>
    public static KeyCommand SwitchToNumericKeypad => new KeyCommand(KeyCommandKind.SwitchToNumericKeypad);

    /// <summary>Open the full-keyboard symbol page.</summary>
    public static KeyCommand SwitchToSymbolPage => new KeyCommand(KeyCommandKind.SwitchToSymbolPage);

    /// <summary>Return to the full-keyboard main page.</summary>
    public static KeyCommand SwitchToMainPage => new KeyCommand(KeyCommandKind.SwitchToMainPage);

    /// <summary>Validate and submit.</summary>
    public static KeyCommand Submit => new KeyCommand(KeyCommandKind.Submit);

    /// <summary>Close and discard.</summary>
    public static KeyCommand Close => new KeyCommand(KeyCommandKind.Close);

    /// <summary>Toggle Chinese/English input.</summary>
    public static KeyCommand ToggleIme => new KeyCommand(KeyCommandKind.ToggleIme);

    /// <inheritdoc />
    public bool Equals(KeyCommand other)
        => Kind == other.Kind && string.Equals(Text, other.Text, StringComparison.Ordinal);

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is KeyCommand other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode()
    {
        unchecked
        {
            return ((int)Kind * 397) ^ (Text?.GetHashCode() ?? 0);
        }
    }

    /// <summary>Equality operator.</summary>
    public static bool operator ==(KeyCommand left, KeyCommand right) => left.Equals(right);

    /// <summary>Inequality operator.</summary>
    public static bool operator !=(KeyCommand left, KeyCommand right) => !left.Equals(right);

    /// <inheritdoc />
    public override string ToString() => Text is null ? Kind.ToString() : $"{Kind}:{Text}";
}
