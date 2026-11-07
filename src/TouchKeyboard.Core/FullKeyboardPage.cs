namespace TouchKeyboard.Core;

/// <summary>
/// Which full-keyboard page is active. The symbol page belongs to the full
/// keyboard only; the numeric keypad never enters it.
/// </summary>
public enum FullKeyboardPage
{
    /// <summary>The main letter page for the selected full-keyboard geometry.</summary>
    Main = 0,

    /// <summary>The common-symbols page owned by the full keyboard.</summary>
    Symbols = 1
}
