using System;
using System.Collections.Generic;
using System.Globalization;

namespace TouchKeyboard.Core;

/// <summary>
/// Builds the immutable layouts used by the core. Layouts describe key order,
/// labels, unit widths and grid cells only; rendering belongs to the frontend.
/// </summary>
public static class KeyboardLayouts
{
    private static readonly KeyboardLayout FullPhoneHybrid = BuildFullPhoneHybrid(imeKey: null);
    private static readonly KeyboardLayout FullAnsiStyle = BuildFullAnsiStyle(imeKey: null);
    private static readonly KeyboardLayout SymbolPage = BuildSymbolPage();
    private static readonly KeyboardLayout GeneralNumpad = BuildNumpad("numeric-general-numpad", NumpadSlots.General);
    private static readonly KeyboardLayout GeneralSequence = BuildGeneralSequence();

    /// <summary>Returns the full keyboard for the requested geometry and page.</summary>
    /// <remarks>
    /// Only <see cref="FullKeyboardLayoutKind.PhoneHybrid"/> has a separate symbol
    /// page in v1. <see cref="FullKeyboardLayoutKind.AnsiStyle"/> keeps every ANSI
    /// symbol on its main page and Shift layer, so it always returns the main page.
    /// This overload has no IME key: Space fills the former IME slot (AnsiStyle 7.0
    /// units, PhoneHybrid 3.5) and row totals stay 15/10.
    /// </remarks>
    public static KeyboardLayout Full(
        FullKeyboardLayoutKind layout,
        FullKeyboardPage page = FullKeyboardPage.Main)
        => Full(layout, page, imeKey: null);

    /// <summary>
    /// Returns the full keyboard with an optional IME toggle key in the former
    /// commandless IME slot. Pass <c>null</c> for the no-IME layout, where Space
    /// expands over the slot.
    /// </summary>
    /// <exception cref="ArgumentException"><paramref name="imeKey"/> does not carry
    /// a <see cref="KeyCommandKind.ToggleIme"/> command, or does not fit the
    /// geometry's IME slot width (1.5 units for AnsiStyle, 1.0 for PhoneHybrid).</exception>
    public static KeyboardLayout Full(
        FullKeyboardLayoutKind layout,
        FullKeyboardPage page,
        KeyDefinition? imeKey)
    {
        if (imeKey is not null)
        {
            if (imeKey.Command.Kind != KeyCommandKind.ToggleIme)
            {
                throw new ArgumentException(
                    "The IME key must dispatch KeyCommandKind.ToggleIme.",
                    nameof(imeKey));
            }

            var slotWidth = layout == FullKeyboardLayoutKind.AnsiStyle ? 1.5 : 1.0;
            if (Math.Abs(imeKey.WidthUnits - slotWidth) > 0.0001)
            {
                throw new ArgumentException(
                    "The IME key width must match the layout slot.",
                    nameof(imeKey));
            }
        }

        switch (layout)
        {
            case FullKeyboardLayoutKind.PhoneHybrid:
                return page == FullKeyboardPage.Symbols
                    ? SymbolPage
                    : BuildFullPhoneHybrid(imeKey);
            case FullKeyboardLayoutKind.AnsiStyle:
                return imeKey is null ? FullAnsiStyle : BuildFullAnsiStyle(imeKey);
            default:
                throw new ArgumentOutOfRangeException(nameof(layout));
        }
    }

    /// <summary>Whether the geometry has a separate symbol page in v1.</summary>
    public static bool HasSymbolPage(FullKeyboardLayoutKind layout)
    {
        switch (layout)
        {
            case FullKeyboardLayoutKind.PhoneHybrid:
                return true;
            case FullKeyboardLayoutKind.AnsiStyle:
                return false;
            default:
                throw new ArgumentOutOfRangeException(nameof(layout));
        }
    }

    /// <summary>Returns the general numeric keypad for the requested surface.</summary>
    public static KeyboardLayout GeneralNumeric(NumericKeypadLayoutKind layout)
    {
        switch (layout)
        {
            case NumericKeypadLayoutKind.Numpad:
                return GeneralNumpad;
            case NumericKeypadLayoutKind.Sequence:
                return GeneralSequence;
            default:
                throw new ArgumentOutOfRangeException(nameof(layout));
        }
    }

    /// <summary>
    /// Builds the restricted numeric Numpad for a numeric profile. Keys the
    /// profile does not accept are not generated; the neighbouring key grows into
    /// the empty slot instead.
    /// </summary>
    public static KeyboardLayout ProfileNumeric(InputProfile profile)
    {
        if (profile is null)
        {
            throw new ArgumentNullException(nameof(profile));
        }

        if (profile.AllowsFullKeyboard)
        {
            throw new ArgumentException("The profile is not a restricted numeric profile.", nameof(profile));
        }

        return BuildNumpad("numeric-profile-" + profile.Id, NumpadSlots.From(profile.NumericKeypad));
    }

    /// <summary>Builds the 5×4 Numpad for an explicit slot mapping.</summary>
    public static KeyboardLayout Numpad(string id, NumpadSlots slots) => BuildNumpad(id, slots);

    private static KeyboardLayout BuildFullAnsiStyle(KeyDefinition? imeKey)
    {
        var spaceWidth = imeKey is null ? 7.0 : 5.5;
        var rows = new List<KeyboardRow>
        {
            Row(
                CharKey('`', '~'),
                CharKey('1', '!'),
                CharKey('2', '@'),
                CharKey('3', '#'),
                CharKey('4', '$'),
                CharKey('5', '%'),
                CharKey('6', '^'),
                CharKey('7', '&'),
                CharKey('8', '*'),
                CharKey('9', '('),
                CharKey('0', ')'),
                CharKey('-', '_'),
                CharKey('=', '+'),
                GlyphKey("key.backspace", Labels.Backspace, KeyCommand.Backspace, KeyGlyph.Backspace, 2.0, KeyStyle.Action, repeatable: true)),

            Row(
                KeyDefinition.Spacer("spacer.tab", 1.5),
                CharKey('q', 'Q'),
                CharKey('w', 'W'),
                CharKey('e', 'E'),
                CharKey('r', 'R'),
                CharKey('t', 'T'),
                CharKey('y', 'Y'),
                CharKey('u', 'U'),
                CharKey('i', 'I'),
                CharKey('o', 'O'),
                CharKey('p', 'P'),
                CharKey('[', '{'),
                CharKey(']', '}'),
                CharKey('\\', '|', widthUnits: 1.5)),

            Row(
                KeyDefinition.Spacer("spacer.caps", 1.75),
                CharKey('a', 'A'),
                CharKey('s', 'S'),
                CharKey('d', 'D'),
                CharKey('f', 'F'),
                CharKey('g', 'G'),
                CharKey('h', 'H'),
                CharKey('j', 'J'),
                CharKey('k', 'K'),
                CharKey('l', 'L'),
                CharKey(';', ':'),
                CharKey('\'', '"'),
                GlyphKey("key.enter", Labels.Enter, KeyCommand.Submit, KeyGlyph.Enter, 2.25, KeyStyle.Action)),

            Row(
                GlyphKey("key.shift.left", Labels.Shift, KeyCommand.ToggleShift, KeyGlyph.Shift, 2.25, KeyStyle.Action),
                CharKey('z', 'Z'),
                CharKey('x', 'X'),
                CharKey('c', 'C'),
                CharKey('v', 'V'),
                CharKey('b', 'B'),
                CharKey('n', 'N'),
                CharKey('m', 'M'),
                CharKey(',', '<'),
                CharKey('.', '>'),
                CharKey('/', '?'),
                GlyphKey("key.shift.right", Labels.Shift, KeyCommand.ToggleShift, KeyGlyph.Shift, 2.75, KeyStyle.Action)),

            BuildAnsiBottomRow(spaceWidth, imeKey)
        };

        return new KeyboardLayout("full-ansi-style", KeyboardMode.FullKeyboard, rows);
    }

    private static KeyboardRow BuildAnsiBottomRow(double spaceWidth, KeyDefinition? imeKey)
    {
        var keys = new List<KeyDefinition>
        {
            NamedKey("key.numeric", Labels.Numbers, KeyCommand.SwitchToNumericKeypad, 1.5, KeyStyle.Action),
            GlyphKey("key.caret.left", Labels.CaretLeft, KeyCommand.MoveCaretLeft, KeyGlyph.ArrowLeft, 1.25, KeyStyle.Action, repeatable: true),
            GlyphKey("key.caret.right", Labels.CaretRight, KeyCommand.MoveCaretRight, KeyGlyph.ArrowRight, 1.25, KeyStyle.Action, repeatable: true),
            NamedKey("key.space", Labels.Space, KeyCommand.Insert(" "), spaceWidth, KeyStyle.Action, glyph: KeyGlyph.Space)
        };

        if (imeKey is not null)
        {
            keys.Add(imeKey);
        }

        keys.Add(GlyphKey("key.clear", Labels.Clear, KeyCommand.Clear, KeyGlyph.Clear, 1.5, KeyStyle.Action));
        keys.Add(NamedKey("key.confirm", Labels.Confirm, KeyCommand.Submit, 2.5, KeyStyle.Primary));
        return Row(keys.ToArray());
    }

    private static KeyboardLayout BuildFullPhoneHybrid(KeyDefinition? imeKey)
    {
        var spaceWidth = imeKey is null ? 3.5 : 2.5;
        var rows = new List<KeyboardRow>
        {
            // The letter width is one unit, fixed by this first row.
            Row(
                CharKey('q', 'Q'),
                CharKey('w', 'W'),
                CharKey('e', 'E'),
                CharKey('r', 'R'),
                CharKey('t', 'T'),
                CharKey('y', 'Y'),
                CharKey('u', 'U'),
                CharKey('i', 'I'),
                CharKey('o', 'O'),
                CharKey('p', 'P')),

            // Half a key of inset on both sides puts A half a key right of Q.
            Row(
                KeyDefinition.Spacer("spacer.inset.left", 0.5),
                CharKey('a', 'A'),
                CharKey('s', 'S'),
                CharKey('d', 'D'),
                CharKey('f', 'F'),
                CharKey('g', 'G'),
                CharKey('h', 'H'),
                CharKey('j', 'J'),
                CharKey('k', 'K'),
                CharKey('l', 'L'),
                KeyDefinition.Spacer("spacer.inset.right", 0.5)),

            Row(
                GlyphKey("key.shift.left", Labels.Shift, KeyCommand.ToggleShift, KeyGlyph.Shift, 1.5, KeyStyle.Action),
                CharKey('z', 'Z'),
                CharKey('x', 'X'),
                CharKey('c', 'C'),
                CharKey('v', 'V'),
                CharKey('b', 'B'),
                CharKey('n', 'N'),
                CharKey('m', 'M'),
                GlyphKey("key.backspace", Labels.Backspace, KeyCommand.Backspace, KeyGlyph.Backspace, 1.5, KeyStyle.Action, repeatable: true)),

            // Caret keys flank the space bar; the space key fills the remaining width.
            BuildPhoneHybridBottomRow(spaceWidth, imeKey)
        };

        return new KeyboardLayout("full-phone-hybrid", KeyboardMode.FullKeyboard, rows);
    }

    private static KeyboardRow BuildPhoneHybridBottomRow(double spaceWidth, KeyDefinition? imeKey)
    {
        var keys = new List<KeyDefinition>
        {
            NamedKey("key.symbols", Labels.Symbols, KeyCommand.SwitchToSymbolPage, style: KeyStyle.Action),
            NamedKey("key.numbers", Labels.Numbers, KeyCommand.SwitchToNumericKeypad, style: KeyStyle.Action),
            GlyphKey("key.caret.left", Labels.CaretLeft, KeyCommand.MoveCaretLeft, KeyGlyph.ArrowLeft, style: KeyStyle.Action, repeatable: true),
            NamedKey("key.space", Labels.Space, KeyCommand.Insert(" "), spaceWidth, KeyStyle.Action, glyph: KeyGlyph.Space),
            GlyphKey("key.caret.right", Labels.CaretRight, KeyCommand.MoveCaretRight, KeyGlyph.ArrowRight, style: KeyStyle.Action, repeatable: true)
        };

        if (imeKey is not null)
        {
            keys.Add(imeKey);
        }

        keys.Add(GlyphKey("key.clear", Labels.Clear, KeyCommand.Clear, KeyGlyph.Clear, style: KeyStyle.Action));
        keys.Add(NamedKey("key.confirm", Labels.Confirm, KeyCommand.Submit, 1.5, KeyStyle.Primary));
        return Row(keys.ToArray());
    }

    private static KeyboardLayout BuildSymbolPage()
    {
        // Tilde moved to the end; the rest of the symbols shift forward one position.
        var rows = new List<KeyboardRow>
        {
            Row(CharKey('!'), CharKey('@'), CharKey('#'), CharKey('$'), CharKey('%'), CharKey('^')),
            Row(CharKey('&'), CharKey('*'), CharKey('('), CharKey(')'), CharKey('_'), CharKey('-')),
            Row(CharKey('+'), CharKey('='), CharKey('['), CharKey(']'), CharKey('{'), CharKey('}')),
            Row(CharKey('\\'), CharKey('|'), CharKey(';'), CharKey(':'), CharKey('\''), CharKey('~')),
            Row(
                NamedKey("key.letters", Labels.Letters, KeyCommand.SwitchToMainPage, style: KeyStyle.Action),
                GlyphKey("key.caret.left", Labels.CaretLeft, KeyCommand.MoveCaretLeft, KeyGlyph.ArrowLeft, style: KeyStyle.Action, repeatable: true),
                NamedKey("key.space", Labels.Space, KeyCommand.Insert(" "), style: KeyStyle.Action, glyph: KeyGlyph.Space),
                GlyphKey("key.caret.right", Labels.CaretRight, KeyCommand.MoveCaretRight, KeyGlyph.ArrowRight, style: KeyStyle.Action, repeatable: true),
                GlyphKey("key.backspace", Labels.Backspace, KeyCommand.Backspace, KeyGlyph.Backspace, style: KeyStyle.Action, repeatable: true),
                NamedKey("key.confirm", Labels.Confirm, KeyCommand.Submit, style: KeyStyle.Primary))
        };

        return new KeyboardLayout("full-symbol-page", KeyboardMode.FullKeyboard, rows);
    }

    private static KeyboardLayout BuildNumpad(string id, NumpadSlots slots)
    {
        if (slots is null)
        {
            throw new ArgumentNullException(nameof(slots));
        }

        var confirmRowSpan = slots.RightRow2 == NumpadSlotKey.None ? 2 : 1;
        var zeroColumnSpan = slots.Separator.HasValue ? 1 : 2;
        var cells = new List<KeyboardGridCell>(20);

        // Row 0: step/plus, 7 8 9, backspace.
        cells.Add(Cell(0, 0, StepKey(slots.ShowStepKeys, up: true)));
        cells.Add(Cell(0, 1, CharKey('7')));
        cells.Add(Cell(0, 2, CharKey('8')));
        cells.Add(Cell(0, 3, CharKey('9')));
        cells.Add(Cell(0, 4, GlyphKey("key.backspace", Labels.Backspace, KeyCommand.Backspace, KeyGlyph.Backspace, style: KeyStyle.Action, repeatable: true)));

        // Row 1: step/minus, 4 5 6, clear.
        cells.Add(Cell(1, 0, StepKey(slots.ShowStepKeys, up: false)));
        cells.Add(Cell(1, 1, CharKey('4')));
        cells.Add(Cell(1, 2, CharKey('5')));
        cells.Add(Cell(1, 3, CharKey('6')));
        cells.Add(Cell(1, 4, GlyphKey("key.clear", Labels.Clear, KeyCommand.Clear, KeyGlyph.Clear, style: KeyStyle.Action)));

        // Row 2: left scenario slot, 1 2 3, right scenario slot.
        if (SlotKey(slots.LeftRow2) is { } left)
        {
            cells.Add(Cell(2, 0, left));
        }

        cells.Add(Cell(2, 1, CharKey('1')));
        cells.Add(Cell(2, 2, CharKey('2')));
        cells.Add(Cell(2, 3, CharKey('3')));

        if (SlotKey(slots.RightRow2) is { } right)
        {
            cells.Add(Cell(2, 4, right));
        }

        // Row 3: caret left, caret right, 0 (widened when the decimal slot is empty),
        // the scenario separator, and Confirm (stretched over an empty right row-2 slot).
        cells.Add(Cell(3, 0, GlyphKey("key.caret.left", Labels.CaretLeft, KeyCommand.MoveCaretLeft, KeyGlyph.ArrowLeft, style: KeyStyle.Action, repeatable: true)));
        cells.Add(Cell(3, 1, GlyphKey("key.caret.right", Labels.CaretRight, KeyCommand.MoveCaretRight, KeyGlyph.ArrowRight, style: KeyStyle.Action, repeatable: true)));
        cells.Add(Cell(3, 2, CharKey('0', columnSpan: zeroColumnSpan)));
        if (slots.Separator is { } separator)
        {
            cells.Add(Cell(3, 3, SeparatorKey(separator)));
        }

        cells.Add(confirmRowSpan == 2
            ? Cell(2, 4, ConfirmKey(2))
            : Cell(3, 4, ConfirmKey(1)));

        return new KeyboardLayout(id, KeyboardMode.NumericKeypad, new KeyboardGridLayout(5, 4, cells));
    }

    private static KeyboardLayout BuildGeneralSequence()
    {
        var rows = new List<KeyboardRow>
        {
            Row(CharKey('1'), CharKey('2'), CharKey('3'), CharKey('4'), CharKey('5'), CharKey('6')),
            Row(CharKey('7'), CharKey('8'), CharKey('9'), CharKey('0'), CharKey('-'), CharKey('/')),
            Row(CharKey(':'), CharKey(';'), CharKey('('), CharKey(')'), CharKey('$'), CharKey('&')),
            Row(CharKey('@'), CharKey('"'), CharKey('.'), CharKey(','), CharKey('?'), CharKey('!')),
            Row(
                NamedKey("key.letters", Labels.Letters, KeyCommand.SwitchToFullKeyboard, style: KeyStyle.Action),
                NamedKey("key.space", Labels.Space, KeyCommand.Insert(" "), 2.0, KeyStyle.Action, glyph: KeyGlyph.Space),
                GlyphKey("key.backspace", Labels.Backspace, KeyCommand.Backspace, KeyGlyph.Backspace, style: KeyStyle.Action, repeatable: true),
                NamedKey("key.confirm", Labels.Confirm, KeyCommand.Submit, 2.0, KeyStyle.Primary))
        };

        return new KeyboardLayout("numeric-general-sequence", KeyboardMode.NumericKeypad, rows);
    }

    private static KeyDefinition StepKey(bool showStepKeys, bool up)
    {
        if (!showStepKeys)
        {
            return up ? CharKey('+', style: KeyStyle.Action) : CharKey('-', style: KeyStyle.Action);
        }

        return up
            ? GlyphKey("key.step.up", Labels.StepUp, KeyCommand.StepDigitUp, KeyGlyph.ArrowUp, style: KeyStyle.Action, repeatable: true)
            : GlyphKey("key.step.down", Labels.StepDown, KeyCommand.StepDigitDown, KeyGlyph.ArrowDown, style: KeyStyle.Action, repeatable: true);
    }

    private static KeyDefinition? SlotKey(NumpadSlotKey slot)
    {
        switch (slot)
        {
            case NumpadSlotKey.None:
                return null;
            case NumpadSlotKey.FullKeyboard:
                return NamedKey("key.letters", Labels.Letters, KeyCommand.SwitchToFullKeyboard, style: KeyStyle.Action);
            case NumpadSlotKey.Sign:
                return NamedKey("key.sign", Labels.Sign, KeyCommand.ToggleSign, style: KeyStyle.Action, glyph: KeyGlyph.PlusMinus);
            default:
                throw new ArgumentOutOfRangeException(nameof(slot));
        }
    }

    private static KeyDefinition SeparatorKey(char separator)
        => new KeyDefinition(
            "key.numpad.separator",
            KeyLabel.Literal(separator.ToString()),
            KeyCommand.Insert(separator.ToString()));

    private static KeyDefinition ConfirmKey(int rowSpan)
        => new KeyDefinition(
            "key.confirm",
            KeyLabel.Resource(Labels.Confirm),
            KeyCommand.Submit,
            style: KeyStyle.Primary,
            rowSpan: rowSpan);

    private static KeyboardGridCell Cell(int row, int column, KeyDefinition key)
        => new KeyboardGridCell(row, column, key);

    private static KeyboardRow Row(params KeyDefinition[] keys) => new KeyboardRow(keys);

    private static KeyDefinition CharKey(
        char c,
        char? shifted = null,
        double widthUnits = 1.0,
        KeyStyle style = KeyStyle.Default,
        bool repeatable = false,
        int columnSpan = 1,
        int rowSpan = 1)
    {
        var shiftedLabel = shifted.HasValue ? KeyLabel.Literal(shifted.Value.ToString()) : (KeyLabel?)null;
        var shiftedCommand = shifted.HasValue ? KeyCommand.Insert(shifted.Value.ToString()) : (KeyCommand?)null;

        return new KeyDefinition(
            CharKeyId(c),
            KeyLabel.Literal(c.ToString()),
            KeyCommand.Insert(c.ToString()),
            widthUnits,
            style,
            repeatable,
            shiftedLabel,
            shiftedCommand,
            columnSpan,
            rowSpan);
    }

    private static KeyDefinition NamedKey(
        string id,
        string resourceKey,
        KeyCommand command,
        double widthUnits = 1.0,
        KeyStyle style = KeyStyle.Action,
        bool repeatable = false,
        KeyGlyph? glyph = null,
        int columnSpan = 1,
        int rowSpan = 1)
        => new KeyDefinition(
            id,
            KeyLabel.Resource(resourceKey),
            command,
            widthUnits,
            style,
            repeatable,
            glyph: glyph,
            columnSpan: columnSpan,
            rowSpan: rowSpan);

    private static KeyDefinition GlyphKey(
        string id,
        string resourceKey,
        KeyCommand command,
        KeyGlyph glyph,
        double widthUnits = 1.0,
        KeyStyle style = KeyStyle.Action,
        bool repeatable = false,
        int columnSpan = 1,
        int rowSpan = 1)
        => NamedKey(id, resourceKey, command, widthUnits, style, repeatable, glyph, columnSpan, rowSpan);

    private static string CharKeyId(char c)
        => "key.char." + ((int)c).ToString("x4", CultureInfo.InvariantCulture);

    private static class Labels
    {
        public const string Backspace = "key.backspace";
        public const string Clear = "key.clear";
        public const string Space = "key.space";
        public const string Confirm = "key.confirm";
        public const string Shift = "key.shift";
        public const string Enter = "key.enter";
        public const string Numbers = "key.numbers";
        public const string Letters = "key.letters";
        public const string Symbols = "key.symbols";
        public const string CaretLeft = "key.caret.left";
        public const string CaretRight = "key.caret.right";
        public const string StepUp = "key.step.up";
        public const string StepDown = "key.step.down";
        public const string Sign = "key.sign";
    }
}
