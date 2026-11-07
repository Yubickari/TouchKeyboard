namespace TouchKeyboard.Core;

/// <summary>
/// A typed semantic icon for a function key. The core only names the icon; each
/// frontend draws its own vector version and keeps the key label for the
/// accessible name and as a text fallback.
/// </summary>
public enum KeyGlyph
{
    /// <summary>Delete the character before the caret.</summary>
    Backspace = 0,

    /// <summary>Clear the whole buffer; drawn as an eraser so it reads differently from Backspace.</summary>
    Clear,

    /// <summary>Return/enter, committing the value.</summary>
    Enter,

    /// <summary>Sticky shift in its inactive (outline) state.</summary>
    Shift,

    /// <summary>Sticky shift in its active (filled) state, selected by the frontend.</summary>
    ShiftFilled,

    /// <summary>Move the caret one character left.</summary>
    ArrowLeft,

    /// <summary>Move the caret one character right.</summary>
    ArrowRight,

    /// <summary>Increment the digit left of the caret.</summary>
    ArrowUp,

    /// <summary>Decrement the digit left of the caret.</summary>
    ArrowDown,

    /// <summary>Return to the full keyboard from the numeric keypad.</summary>
    Keyboard,

    /// <summary>Open the symbol page; optional for frontends that show a word instead.</summary>
    Symbol,

    /// <summary>Open the numeric keypad; optional for frontends that show a word instead.</summary>
    Numeric,

    /// <summary>Language/IME slot, reserved for a future input-method switch.</summary>
    Ime,

    /// <summary>The space bar.</summary>
    Space,

    /// <summary>Reveal a masked password.</summary>
    ShowPassword,

    /// <summary>Mask a revealed password again.</summary>
    HidePassword,

    /// <summary>The plus/minus (sign toggle) symbol.</summary>
    PlusMinus
}
