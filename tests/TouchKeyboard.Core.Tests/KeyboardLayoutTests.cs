using System.Linq;
using TouchKeyboard.Core;
using Xunit;

namespace TouchKeyboard.Core.Tests;

public sealed class KeyboardLayoutTests
{
    private static IEnumerable<KeyDefinition> Flatten(KeyboardLayout layout)
        => layout.Rows.SelectMany(row => row.Keys);

    [Fact]
    public void Phone_hybrid_full_keyboard_has_the_expected_rows_and_units()
    {
        var layout = KeyboardLayouts.Full(FullKeyboardLayoutKind.PhoneHybrid);

        Assert.Equal(KeyboardMode.FullKeyboard, layout.Mode);
        Assert.Equal(5, layout.Rows.Count);
        Assert.Equal(new[] { 15d, 13d, 13d, 14d, 15d }, layout.Rows.Select(row => row.TotalUnits));
        Assert.Equal(57, layout.Rows.Sum(row => row.Keys.Count));
    }

    [Fact]
    public void Full_keyboard_keeps_shifted_commands_for_symbols()
    {
        var layout = KeyboardLayouts.Full(FullKeyboardLayoutKind.PhoneHybrid);

        Assert.Contains(Flatten(layout), key =>
            key.Command == KeyCommand.Insert("1")
            && key.ShiftedCommand.HasValue
            && key.ShiftedCommand.Value == KeyCommand.Insert("!"));

        Assert.Contains(Flatten(layout), key =>
            key.Command == KeyCommand.Insert("[")
            && key.ShiftedCommand.HasValue
            && key.ShiftedCommand.Value == KeyCommand.Insert("{"));

        Assert.Contains(Flatten(layout), key => key.Command.Kind == KeyCommandKind.SwitchToNumericKeypad);
        Assert.Contains(Flatten(layout), key => key.Command.Kind == KeyCommandKind.ToggleShift);
        Assert.Contains(Flatten(layout), key => key.Command.Kind == KeyCommandKind.Submit);
        Assert.Contains(Flatten(layout), key => key.Command.Kind == KeyCommandKind.Backspace && key.Repeatable);
    }

    [Fact]
    public void Ansi_strict_is_reserved()
        => Assert.Throws<NotSupportedException>(
            () => KeyboardLayouts.Full(FullKeyboardLayoutKind.AnsiStrict));

    [Fact]
    public void General_numpad_has_six_columns_and_the_general_keys()
    {
        var layout = KeyboardLayouts.GeneralNumeric(NumericKeypadLayoutKind.Numpad);

        Assert.Equal(KeyboardMode.NumericKeypad, layout.Mode);
        Assert.Equal(4, layout.Rows.Count);
        Assert.All(layout.Rows, row => Assert.Equal(6d, row.TotalUnits));

        var keys = Flatten(layout).ToList();
        Assert.Contains(keys, key => key.Command.Kind == KeyCommandKind.SwitchToFullKeyboard);
        Assert.Contains(keys, key => key.Command.Kind == KeyCommandKind.InsertText && key.Command.Text == "0");
        Assert.Contains(keys, key => key.Command.Kind == KeyCommandKind.InsertText && key.Command.Text == "@");
        Assert.Contains(keys, key => key.Command.Kind == KeyCommandKind.Submit);
        Assert.Contains(keys, key => key.Command.Kind == KeyCommandKind.Clear);
        Assert.DoesNotContain(keys, key => key.Style == KeyStyle.Spacer);
    }

    [Fact]
    public void General_phone_has_five_rows_and_the_phone_symbols()
    {
        var layout = KeyboardLayouts.GeneralNumeric(NumericKeypadLayoutKind.Phone);

        Assert.Equal(5, layout.Rows.Count);
        Assert.All(layout.Rows, row => Assert.Equal(6d, row.TotalUnits));

        var keys = Flatten(layout).ToList();
        Assert.Contains(keys, key => key.Command.Kind == KeyCommandKind.SwitchToFullKeyboard);
        Assert.Contains(keys, key => key.Command.Kind == KeyCommandKind.Submit);
        Assert.Contains(keys, key => key.Command.Kind == KeyCommandKind.InsertText && key.Command.Text == "$");
        Assert.Contains(keys, key => key.Command.Kind == KeyCommandKind.InsertText && key.Command.Text == "?");
        Assert.DoesNotContain(keys, key => key.Command.Kind == KeyCommandKind.Clear);
    }

    [Fact]
    public void Positive_integer_keypad_omits_sign_and_separator()
    {
        var layout = KeyboardLayouts.ProfileNumeric(InputProfiles.PositiveInteger);
        var commands = Flatten(layout)
            .Where(key => key.Style != KeyStyle.Spacer)
            .Select(key => key.Command)
            .ToList();

        Assert.Equal(4, layout.Rows.Count);
        Assert.All(layout.Rows, row => Assert.Equal(5d, row.TotalUnits));
        Assert.Contains(commands, command => command.Kind == KeyCommandKind.StepDigitUp);
        Assert.Contains(commands, command => command.Kind == KeyCommandKind.StepDigitDown);
        Assert.Contains(commands, command => command.Kind == KeyCommandKind.Submit);
        Assert.Contains(commands, command => command.Kind == KeyCommandKind.InsertText && command.Text == "0");
        Assert.DoesNotContain(commands, command => command.Kind == KeyCommandKind.ToggleSign);
        Assert.DoesNotContain(commands, command => command.Kind == KeyCommandKind.SwitchToFullKeyboard);
        Assert.DoesNotContain(commands, command => command.Kind == KeyCommandKind.InsertText && command.Text == ".");
        Assert.DoesNotContain(commands, command => command.Kind == KeyCommandKind.InsertText && command.Text == " ");
    }

    [Fact]
    public void Signed_integer_keypad_adds_the_sign_key()
    {
        var commands = Flatten(KeyboardLayouts.ProfileNumeric(InputProfiles.Integer))
            .Select(key => key.Command)
            .ToList();

        Assert.Contains(commands, command => command.Kind == KeyCommandKind.ToggleSign);
        Assert.DoesNotContain(commands, command => command.Kind == KeyCommandKind.InsertText && command.Text == ".");
    }

    [Fact]
    public void Decimal_keypads_add_the_decimal_point_and_sign_only_when_allowed()
    {
        var signed = Flatten(KeyboardLayouts.ProfileNumeric(InputProfiles.Decimal))
            .Select(key => key.Command)
            .ToList();
        var positive = Flatten(KeyboardLayouts.ProfileNumeric(InputProfiles.PositiveDecimal))
            .Select(key => key.Command)
            .ToList();

        Assert.Contains(signed, command => command.Kind == KeyCommandKind.ToggleSign);
        Assert.Contains(signed, command => command.Kind == KeyCommandKind.InsertText && command.Text == ".");
        Assert.DoesNotContain(positive, command => command.Kind == KeyCommandKind.ToggleSign);
        Assert.Contains(positive, command => command.Kind == KeyCommandKind.InsertText && command.Text == ".");
    }

    [Fact]
    public void Date_and_ip_keypads_expose_the_required_separator()
    {
        var slash = Flatten(KeyboardLayouts.ProfileNumeric(InputProfiles.DateSlash))
            .Select(key => key.Command)
            .ToList();
        var dotDate = Flatten(KeyboardLayouts.ProfileNumeric(InputProfiles.DateDot))
            .Select(key => key.Command)
            .ToList();
        var ip = Flatten(KeyboardLayouts.ProfileNumeric(InputProfiles.IpAddress))
            .Select(key => key.Command)
            .ToList();

        Assert.Contains(slash, command => command.Kind == KeyCommandKind.InsertText && command.Text == "/");
        Assert.DoesNotContain(slash, command => command.Kind == KeyCommandKind.InsertText && command.Text == ".");
        Assert.Contains(dotDate, command => command.Kind == KeyCommandKind.InsertText && command.Text == ".");
        Assert.Contains(ip, command => command.Kind == KeyCommandKind.InsertText && command.Text == ".");
        Assert.DoesNotContain(slash, command => command.Kind == KeyCommandKind.ToggleSign);
        Assert.DoesNotContain(ip, command => command.Kind == KeyCommandKind.ToggleSign);
    }

    [Fact]
    public void Profile_numeric_rejects_a_full_keyboard_profile()
        => Assert.Throws<ArgumentException>(
            () => KeyboardLayouts.ProfileNumeric(InputProfiles.Text));

    [Fact]
    public void Spacers_have_no_command()
    {
        var spacers = Flatten(KeyboardLayouts.ProfileNumeric(InputProfiles.PositiveInteger))
            .Where(key => key.Style == KeyStyle.Spacer)
            .ToList();

        Assert.NotEmpty(spacers);
        Assert.All(spacers, spacer =>
        {
            Assert.Equal(KeyCommandKind.None, spacer.Command.Kind);
            Assert.Equal(string.Empty, spacer.Label.Text);
        });
    }

    [Fact]
    public void Every_layout_has_unique_key_ids_and_numeric_rows_have_five_units()
    {
        var layouts = new List<KeyboardLayout>
        {
            KeyboardLayouts.Full(FullKeyboardLayoutKind.PhoneHybrid),
            KeyboardLayouts.GeneralNumeric(NumericKeypadLayoutKind.Numpad),
            KeyboardLayouts.GeneralNumeric(NumericKeypadLayoutKind.Phone)
        };

        foreach (var profile in InputProfiles.All.Where(profile => !profile.AllowsFullKeyboard))
        {
            var numeric = KeyboardLayouts.ProfileNumeric(profile);
            Assert.All(numeric.Rows, row => Assert.Equal(5d, row.TotalUnits));
            layouts.Add(numeric);
        }

        foreach (var layout in layouts)
        {
            var ids = Flatten(layout).Select(key => key.Id).ToList();
            Assert.Equal(ids.Count, ids.Distinct(StringComparer.Ordinal).Count());
        }
    }
}
