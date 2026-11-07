using System;

namespace TouchKeyboard.Core;

/// <summary>
/// The scenario mapping for a <see cref="KeyboardGridLayout"/> Numpad: whether the
/// top-left pair steps digits or inserts +/-, which semantic keys fill the row-2
/// side slots, and which separator (if any) fills the bottom decimal slot. The
/// 3×4 digit block, the caret keys, Backspace, Clear and Confirm are fixed.
/// </summary>
public sealed class NumpadSlots
{
    /// <summary>Creates a slot mapping.</summary>
    /// <param name="showStepKeys">
    /// True puts the ▲/▼ step keys above the left column; false puts the +/−
    /// characters the field accepts as text.
    /// </param>
    /// <param name="leftRow2">The semantic key at the left row-2 slot.</param>
    /// <param name="separator">The bottom decimal/date/IP separator, or null to leave the slot empty.</param>
    /// <param name="rightRow2">The semantic key at the right row-2 slot.</param>
    public NumpadSlots(
        bool showStepKeys,
        NumpadSlotKey leftRow2 = NumpadSlotKey.None,
        char? separator = null,
        NumpadSlotKey rightRow2 = NumpadSlotKey.None)
    {
        ShowStepKeys = showStepKeys;
        LeftRow2 = leftRow2;
        Separator = separator;
        RightRow2 = rightRow2;
    }

    /// <summary>Whether the top-left pair steps digits instead of inserting +/−.</summary>
    public bool ShowStepKeys { get; }

    /// <summary>The semantic key at the left row-2 slot.</summary>
    public NumpadSlotKey LeftRow2 { get; }

    /// <summary>The semantic key at the right row-2 slot.</summary>
    public NumpadSlotKey RightRow2 { get; }

    /// <summary>The bottom separator, or null when 0 widens over the empty slot.</summary>
    public char? Separator { get; }

    /// <summary>
    /// The general numeric surface reached from the full keyboard: +/− as text,
    /// ABC at the left row-2 slot, a decimal point and a free right row-2 slot.
    /// </summary>
    public static NumpadSlots General { get; } = From(NumericKeypadOptions.General);

    /// <summary>
    /// Derives the slots of a restricted numeric profile from its keypad options.
    /// The profile's sign flag picks ± over ABC, and a decimal point wins over an
    /// explicit date/IP separator.
    /// </summary>
    public static NumpadSlots From(NumericKeypadOptions options)
    {
        if (options is null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        var leftRow2 = options.ShowSign
            ? NumpadSlotKey.Sign
            : options.ShowFullKeyboardSwitch
                ? NumpadSlotKey.FullKeyboard
                : NumpadSlotKey.None;

        var separator = options.ShowDecimalPoint ? (char?)'.' : options.Separator;

        return new NumpadSlots(options.ShowStepKeys, leftRow2, separator);
    }
}
