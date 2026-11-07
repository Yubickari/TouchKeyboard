using System;
using System.Collections.Generic;

namespace TouchKeyboard.Core;

/// <summary>
/// Which extra keys a numeric profile exposes. <see cref="NumpadSlots"/> derives
/// the Numpad slot mapping from the step, sign, decimal-point, separator and
/// full-keyboard-switch flags; the remaining flags are retained for source
/// compatibility with the legacy key-set description.
/// </summary>
public sealed class NumericKeypadOptions
{
    /// <summary>Creates numeric keypad options.</summary>
    public NumericKeypadOptions(
        bool showSign = false,
        bool showDecimalPoint = false,
        char? separator = null,
        bool showStepKeys = false,
        bool showCaretKeys = true,
        bool showFullKeyboardSwitch = false,
        bool showSpace = false,
        IReadOnlyList<char>? symbolKeys = null)
    {
        ShowSign = showSign;
        ShowDecimalPoint = showDecimalPoint;
        Separator = separator;
        ShowStepKeys = showStepKeys;
        ShowCaretKeys = showCaretKeys;
        ShowFullKeyboardSwitch = showFullKeyboardSwitch;
        ShowSpace = showSpace;

        var symbols = new List<char>();
        if (symbolKeys is not null)
        {
            foreach (var symbol in symbolKeys)
            {
                symbols.Add(symbol);
            }
        }

        SymbolKeys = symbols.AsReadOnly();
    }

    /// <summary>Whether the +/- toggle is available.</summary>
    public bool ShowSign { get; }

    /// <summary>Whether a decimal point key is available.</summary>
    public bool ShowDecimalPoint { get; }

    /// <summary>A date or IPv4 separator key, when one is needed.</summary>
    public char? Separator { get; }

    /// <summary>Whether step-up/step-down keys are available.</summary>
    public bool ShowStepKeys { get; }

    /// <summary>Whether caret left/right keys are available.</summary>
    public bool ShowCaretKeys { get; }

    /// <summary>Whether the ABC (full keyboard) switch is available.</summary>
    public bool ShowFullKeyboardSwitch { get; }

    /// <summary>Whether a space key is available.</summary>
    public bool ShowSpace { get; }

    /// <summary>Symbols shown by the general numeric keypad.</summary>
    public IReadOnlyList<char> SymbolKeys { get; }

    /// <summary>The general numeric keypad: digits plus common symbols.</summary>
    public static NumericKeypadOptions General { get; } = new NumericKeypadOptions(
        showDecimalPoint: true,
        showCaretKeys: true,
        showFullKeyboardSwitch: true,
        showSpace: true,
        symbolKeys: new[] { '-', '/', ':', '@', ',' });

    /// <summary>Options for an integer profile.</summary>
    public static NumericKeypadOptions Integer(bool allowNegative)
        => new NumericKeypadOptions(showSign: allowNegative, showStepKeys: true);

    /// <summary>Options for a decimal profile.</summary>
    public static NumericKeypadOptions Decimal(bool allowNegative)
        => new NumericKeypadOptions(showSign: allowNegative, showDecimalPoint: true, showStepKeys: true);

    /// <summary>Options for a date profile with the given separator.</summary>
    public static NumericKeypadOptions Date(char separator)
        => new NumericKeypadOptions(separator: separator, showStepKeys: true);

    /// <summary>Options for an IPv4 profile.</summary>
    public static NumericKeypadOptions IpAddress()
        => new NumericKeypadOptions(separator: '.', showStepKeys: true);
}
