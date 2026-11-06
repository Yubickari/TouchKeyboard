using System;
using System.Collections.Generic;
using System.Globalization;

namespace TouchKeyboard.Core;

/// <summary>
/// Builds the immutable layouts used by the core. Layouts describe key order,
/// labels and unit widths only; rendering belongs to the frontend.
/// </summary>
public static class KeyboardLayouts
{
    private static readonly KeyboardLayout FullPhoneHybrid = BuildFullPhoneHybrid();
    private static readonly KeyboardLayout GeneralNumpad = BuildGeneralNumpad();
    private static readonly KeyboardLayout GeneralPhone = BuildGeneralPhone();

    /// <summary>Returns the full keyboard for the requested geometry.</summary>
    public static KeyboardLayout Full(FullKeyboardLayoutKind layout)
    {
        switch (layout)
        {
            case FullKeyboardLayoutKind.PhoneHybrid:
                return FullPhoneHybrid;
            case FullKeyboardLayoutKind.AnsiStrict:
                throw new NotSupportedException("The AnsiStrict layout is reserved for a future release.");
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
            case NumericKeypadLayoutKind.Phone:
                return GeneralPhone;
            default:
                throw new ArgumentOutOfRangeException(nameof(layout));
        }
    }

    /// <summary>
    /// Builds the restricted numeric keypad for a numeric profile. Keys that the
    /// profile does not accept are replaced by empty spacers rather than disabled
    /// buttons.
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

        return BuildProfileNumeric(profile);
    }

    private static KeyboardLayout BuildFullPhoneHybrid()
    {
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
                NamedKey("key.backspace", Labels.Backspace, KeyCommand.Backspace, 2.0, KeyStyle.Action, repeatable: true)),

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
                CharKey('p', 'P'),
                CharKey('[', '{'),
                CharKey(']', '}'),
                CharKey('\\', '|')),

            Row(
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
                NamedKey("key.enter", Labels.Enter, KeyCommand.Submit, 2.0, KeyStyle.Action)),

            Row(
                NamedKey("key.shift.left", Labels.Shift, KeyCommand.ToggleShift, 2.0, KeyStyle.Action),
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
                NamedKey("key.shift.right", Labels.Shift, KeyCommand.ToggleShift, 2.0, KeyStyle.Action)),

            Row(
                NamedKey("key.numeric", Labels.Numbers, KeyCommand.SwitchToNumericKeypad, 1.5, KeyStyle.Action),
                NamedKey("key.caret.left", Labels.CaretLeft, KeyCommand.MoveCaretLeft, 1.25, KeyStyle.Action, repeatable: true),
                NamedKey("key.caret.right", Labels.CaretRight, KeyCommand.MoveCaretRight, 1.25, KeyStyle.Action, repeatable: true),
                NamedKey("key.space", Labels.Space, KeyCommand.Insert(" "), 7.0, KeyStyle.Action),
                NamedKey("key.clear", Labels.Clear, KeyCommand.Clear, 1.5, KeyStyle.Action),
                NamedKey("key.confirm", Labels.Confirm, KeyCommand.Submit, 2.5, KeyStyle.Primary))
        };

        return new KeyboardLayout("full-phone-hybrid", KeyboardMode.FullKeyboard, rows);
    }

    private static KeyboardLayout BuildGeneralNumpad()
    {
        var rows = new List<KeyboardRow>
        {
            Row(
                NamedKey("key.caret.left", Labels.CaretLeft, KeyCommand.MoveCaretLeft, style: KeyStyle.Action, repeatable: true),
                CharKey('7'),
                CharKey('8'),
                CharKey('9'),
                NamedKey("key.backspace", Labels.Backspace, KeyCommand.Backspace, style: KeyStyle.Action, repeatable: true),
                NamedKey("key.clear", Labels.Clear, KeyCommand.Clear, style: KeyStyle.Action)),

            Row(
                NamedKey("key.caret.right", Labels.CaretRight, KeyCommand.MoveCaretRight, style: KeyStyle.Action, repeatable: true),
                CharKey('4'),
                CharKey('5'),
                CharKey('6'),
                CharKey('.'),
                CharKey('-')),

            Row(
                NamedKey("key.letters", Labels.Letters, KeyCommand.SwitchToFullKeyboard, style: KeyStyle.Action),
                CharKey('1'),
                CharKey('2'),
                CharKey('3'),
                CharKey('/'),
                CharKey(':')),

            Row(
                CharKey(','),
                CharKey('0'),
                CharKey('@'),
                NamedKey("key.space", Labels.Space, KeyCommand.Insert(" "), style: KeyStyle.Action),
                NamedKey("key.confirm", Labels.Confirm, KeyCommand.Submit, 2.0, KeyStyle.Primary))
        };

        return new KeyboardLayout("numeric-general-numpad", KeyboardMode.NumericKeypad, rows);
    }

    private static KeyboardLayout BuildGeneralPhone()
    {
        var rows = new List<KeyboardRow>
        {
            Row(CharKey('1'), CharKey('2'), CharKey('3'), CharKey('4'), CharKey('5'), CharKey('6')),
            Row(CharKey('7'), CharKey('8'), CharKey('9'), CharKey('0'), CharKey('-'), CharKey('/')),
            Row(CharKey(':'), CharKey(';'), CharKey('('), CharKey(')'), CharKey('$'), CharKey('&')),
            Row(CharKey('@'), CharKey('"'), CharKey('.'), CharKey(','), CharKey('?'), CharKey('!')),
            Row(
                NamedKey("key.letters", Labels.Letters, KeyCommand.SwitchToFullKeyboard, style: KeyStyle.Action),
                NamedKey("key.space", Labels.Space, KeyCommand.Insert(" "), 2.0, KeyStyle.Action),
                NamedKey("key.backspace", Labels.Backspace, KeyCommand.Backspace, style: KeyStyle.Action, repeatable: true),
                NamedKey("key.confirm", Labels.Confirm, KeyCommand.Submit, 2.0, KeyStyle.Primary))
        };

        return new KeyboardLayout("numeric-general-phone", KeyboardMode.NumericKeypad, rows);
    }

    private static KeyboardLayout BuildProfileNumeric(InputProfile profile)
    {
        var options = profile.NumericKeypad;
        var separator = options.ShowDecimalPoint ? (char?)'.' : options.Separator;
        var id = "numeric-profile-" + profile.Id;

        var row0 = new List<KeyDefinition>
        {
            options.ShowCaretKeys
                ? NamedKey("key.caret.left", Labels.CaretLeft, KeyCommand.MoveCaretLeft, style: KeyStyle.Action, repeatable: true)
                : KeyDefinition.Spacer("spacer.r0.c0"),
            CharKey('7'),
            CharKey('8'),
            CharKey('9'),
            NamedKey("key.backspace", Labels.Backspace, KeyCommand.Backspace, style: KeyStyle.Action, repeatable: true)
        };

        var row1 = new List<KeyDefinition>
        {
            options.ShowCaretKeys
                ? NamedKey("key.caret.right", Labels.CaretRight, KeyCommand.MoveCaretRight, style: KeyStyle.Action, repeatable: true)
                : KeyDefinition.Spacer("spacer.r1.c0"),
            CharKey('4'),
            CharKey('5'),
            CharKey('6'),
            NamedKey("key.clear", Labels.Clear, KeyCommand.Clear, style: KeyStyle.Action)
        };

        var row2 = new List<KeyDefinition>
        {
            options.ShowStepKeys
                ? NamedKey("key.step.up", Labels.StepUp, KeyCommand.StepDigitUp, style: KeyStyle.Action, repeatable: true)
                : KeyDefinition.Spacer("spacer.r2.c0"),
            CharKey('1'),
            CharKey('2'),
            CharKey('3'),
            options.ShowSign
                ? NamedKey("key.sign", Labels.Sign, KeyCommand.ToggleSign, style: KeyStyle.Action)
                : KeyDefinition.Spacer("spacer.r2.c4")
        };

        var row3 = new List<KeyDefinition>
        {
            options.ShowStepKeys
                ? NamedKey("key.step.down", Labels.StepDown, KeyCommand.StepDigitDown, style: KeyStyle.Action, repeatable: true)
                : KeyDefinition.Spacer("spacer.r3.c0")
        };

        if (separator.HasValue)
        {
            row3.Add(CharKey('0'));
            row3.Add(CharKey(separator.Value));
        }
        else
        {
            row3.Add(CharKey('0', widthUnits: 2.0));
        }

        row3.Add(NamedKey("key.confirm", Labels.Confirm, KeyCommand.Submit, 2.0, KeyStyle.Primary));

        var rows = new List<KeyboardRow>
        {
            new KeyboardRow(row0),
            new KeyboardRow(row1),
            new KeyboardRow(row2),
            new KeyboardRow(row3)
        };

        return new KeyboardLayout(id, KeyboardMode.NumericKeypad, rows);
    }

    private static KeyboardRow Row(params KeyDefinition[] keys) => new KeyboardRow(keys);

    private static KeyDefinition CharKey(
        char c,
        char? shifted = null,
        double widthUnits = 1.0,
        KeyStyle style = KeyStyle.Default,
        bool repeatable = false)
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
            shiftedCommand);
    }

    private static KeyDefinition NamedKey(
        string id,
        string resourceKey,
        KeyCommand command,
        double widthUnits = 1.0,
        KeyStyle style = KeyStyle.Action,
        bool repeatable = false)
        => new KeyDefinition(id, KeyLabel.Resource(resourceKey), command, widthUnits, style, repeatable);

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
        public const string CaretLeft = "key.caret.left";
        public const string CaretRight = "key.caret.right";
        public const string StepUp = "key.step.up";
        public const string StepDown = "key.step.down";
        public const string Sign = "key.sign";
    }
}
