using System;
using System.Collections.Generic;
using System.Linq;
using TouchKeyboard.Core;
using Xunit;

namespace TouchKeyboard.Core.Tests;

public sealed class KeyboardLayoutTests
{
    private static IEnumerable<KeyDefinition> Flatten(KeyboardLayout layout)
        => layout.IsGrid
            ? layout.Grid!.Cells.Select(cell => cell.Key)
            : layout.Rows.SelectMany(row => row.Keys);

    private static KeyDefinition At(KeyboardLayout layout, int row, int column)
        => layout.Grid!.Cells.Single(cell => cell.Row == row && cell.Column == column).Key;

    private static KeyboardGridCell CellAt(KeyboardLayout layout, int row, int column)
        => layout.Grid!.Cells.Single(cell => cell.Row == row && cell.Column == column);

    private static IEnumerable<KeyboardLayout> AllLayouts()
    {
        yield return KeyboardLayouts.Full(FullKeyboardLayoutKind.AnsiStyle);
        yield return KeyboardLayouts.Full(FullKeyboardLayoutKind.PhoneHybrid);
        yield return KeyboardLayouts.Full(FullKeyboardLayoutKind.PhoneHybrid, FullKeyboardPage.Symbols);
        yield return KeyboardLayouts.GeneralNumeric(NumericKeypadLayoutKind.Numpad);
        yield return KeyboardLayouts.GeneralNumeric(NumericKeypadLayoutKind.Sequence);

        foreach (var profile in InputProfiles.All.Where(profile => !profile.AllowsFullKeyboard))
        {
            yield return KeyboardLayouts.ProfileNumeric(profile);
        }
    }

    [Fact]
    public void Ansi_style_has_five_rows_of_fifteen_units()
    {
        var layout = KeyboardLayouts.Full(FullKeyboardLayoutKind.AnsiStyle);

        Assert.Equal("full-ansi-style", layout.Id);
        Assert.Equal(KeyboardMode.FullKeyboard, layout.Mode);
        Assert.False(layout.IsGrid);
        Assert.Equal(5, layout.Rows.Count);
        Assert.Equal(new[] { 15d, 15d, 15d, 15d, 15d }, layout.Rows.Select(row => row.TotalUnits));
    }

    [Fact]
    public void Ansi_style_reserves_tab_and_caps_but_no_commandless_ime_spacer()
    {
        var keys = Flatten(KeyboardLayouts.Full(FullKeyboardLayoutKind.AnsiStyle)).ToList();

        Assert.Contains(keys, key => key.Id == "spacer.tab" && key.Style == KeyStyle.Spacer
            && key.WidthUnits == 1.5 && key.Command.Kind == KeyCommandKind.None);
        Assert.Contains(keys, key => key.Id == "spacer.caps" && key.Style == KeyStyle.Spacer
            && key.WidthUnits == 1.75 && key.Command.Kind == KeyCommandKind.None);
        Assert.DoesNotContain(keys, key => key.Id == "spacer.ime");
        Assert.All(keys.Where(key => key.Style == KeyStyle.Spacer), spacer =>
        {
            Assert.Equal(KeyCommandKind.None, spacer.Command.Kind);
            Assert.Null(spacer.Glyph);
        });
    }

    [Fact]
    public void No_ime_space_expands_over_the_former_ime_slot()
    {
        var ansi = KeyboardLayouts.Full(FullKeyboardLayoutKind.AnsiStyle).Rows[4];
        var phone = KeyboardLayouts.Full(FullKeyboardLayoutKind.PhoneHybrid).Rows[3];

        Assert.Equal(7.0, ansi.Keys.Single(key => key.Id == "key.space").WidthUnits);
        Assert.Equal(3.5, phone.Keys.Single(key => key.Id == "key.space").WidthUnits);
        Assert.Equal(15d, ansi.TotalUnits);
        Assert.Equal(10d, phone.TotalUnits);
        Assert.DoesNotContain(ansi.Keys, key => key.Command.Kind == KeyCommandKind.ToggleIme);
        Assert.DoesNotContain(phone.Keys, key => key.Command.Kind == KeyCommandKind.ToggleIme);
    }

    [Fact]
    public void Ansi_style_uses_the_exact_enter_shift_and_backslash_widths()
    {
        var keys = Flatten(KeyboardLayouts.Full(FullKeyboardLayoutKind.AnsiStyle)).ToList();

        Assert.Equal(2.0, keys.Single(key => key.Id == "key.backspace").WidthUnits);
        Assert.Equal(2.25, keys.Single(key => key.Id == "key.enter").WidthUnits);
        Assert.Equal(1.5, keys.Single(key => key.Id == "key.char.005c").WidthUnits);
        Assert.Equal(2.25, keys.Single(key => key.Id == "key.shift.left").WidthUnits);
        Assert.Equal(2.75, keys.Single(key => key.Id == "key.shift.right").WidthUnits);
    }

    [Fact]
    public void Ansi_style_keeps_the_full_ansi_shift_layers()
    {
        var keys = Flatten(KeyboardLayouts.Full(FullKeyboardLayoutKind.AnsiStyle)).ToList();

        Assert.Contains(keys, key => key.Command == KeyCommand.Insert("`")
            && key.ShiftedCommand == KeyCommand.Insert("~"));
        Assert.Contains(keys, key => key.Command == KeyCommand.Insert("1")
            && key.ShiftedCommand == KeyCommand.Insert("!"));
        Assert.Contains(keys, key => key.Command == KeyCommand.Insert("[")
            && key.ShiftedCommand == KeyCommand.Insert("{"));
        Assert.Contains(keys, key => key.Command == KeyCommand.Insert(";")
            && key.ShiftedCommand == KeyCommand.Insert(":"));
        Assert.Contains(keys, key => key.Command == KeyCommand.Insert(",")
            && key.ShiftedCommand == KeyCommand.Insert("<"));
        Assert.Contains(keys, key => key.Command == KeyCommand.Insert("/")
            && key.ShiftedCommand == KeyCommand.Insert("?"));
    }

    [Fact]
    public void Ansi_style_bottom_row_expands_space_and_has_no_ime_key()
    {
        var layout = KeyboardLayouts.Full(FullKeyboardLayoutKind.AnsiStyle);
        var row = layout.Rows[4];

        Assert.Equal(15d, row.TotalUnits);
        Assert.Equal(6, row.Keys.Count);
        Assert.Equal(KeyCommandKind.SwitchToNumericKeypad, row.Keys[0].Command.Kind);
        Assert.Equal(1.5, row.Keys[0].WidthUnits);
        Assert.Equal(KeyGlyph.ArrowLeft, row.Keys[1].Glyph);
        Assert.Equal(KeyGlyph.ArrowRight, row.Keys[2].Glyph);
        Assert.Equal(7.0, row.Keys[3].WidthUnits);
        Assert.Equal(KeyCommand.Insert(" "), row.Keys[3].Command);
        Assert.Equal(KeyGlyph.Space, row.Keys[3].Glyph);
        Assert.Equal(KeyGlyph.Clear, row.Keys[4].Glyph);
        Assert.Equal(KeyCommandKind.Submit, row.Keys[5].Command.Kind);
        Assert.Equal(KeyStyle.Primary, row.Keys[5].Style);
        Assert.Equal(2.5, row.Keys[5].WidthUnits);
        Assert.DoesNotContain(row.Keys, key => key.Style == KeyStyle.Spacer);
    }

    [Fact]
    public void Ansi_style_icon_keys_carry_a_glyph_and_a_localized_label()
    {
        var keys = Flatten(KeyboardLayouts.Full(FullKeyboardLayoutKind.AnsiStyle)).ToList();

        AssertGlyphKey(keys, "key.backspace", KeyGlyph.Backspace, KeyCommandKind.Backspace, repeatable: true);
        AssertGlyphKey(keys, "key.enter", KeyGlyph.Enter, KeyCommandKind.Submit);
        AssertGlyphKey(keys, "key.shift.left", KeyGlyph.Shift, KeyCommandKind.ToggleShift, resourceKey: "key.shift");
        AssertGlyphKey(keys, "key.shift.right", KeyGlyph.Shift, KeyCommandKind.ToggleShift, resourceKey: "key.shift");
        AssertGlyphKey(keys, "key.clear", KeyGlyph.Clear, KeyCommandKind.Clear);
        AssertGlyphKey(keys, "key.caret.left", KeyGlyph.ArrowLeft, KeyCommandKind.MoveCaretLeft, repeatable: true);
        AssertGlyphKey(keys, "key.caret.right", KeyGlyph.ArrowRight, KeyCommandKind.MoveCaretRight, repeatable: true);
    }

    [Fact]
    public void Phone_hybrid_has_four_rows_and_no_number_row()
    {
        var layout = KeyboardLayouts.Full(FullKeyboardLayoutKind.PhoneHybrid);

        Assert.Equal("full-phone-hybrid", layout.Id);
        Assert.Equal(4, layout.Rows.Count);
        Assert.Equal(new[] { 10d, 10d, 10d, 10d }, layout.Rows.Select(row => row.TotalUnits));

        var inserts = Flatten(layout)
            .Where(key => key.Command.Kind == KeyCommandKind.InsertText)
            .Select(key => key.Command.Text)
            .ToList();

        Assert.DoesNotContain(inserts, text => text is "1" or "2" or "3" or "4" or "5" or "6" or "7" or "8" or "9" or "0");
        Assert.Contains(inserts, text => text == "q");
        Assert.Contains(inserts, text => text == " ");
    }

    [Fact]
    public void Phone_hybrid_insets_the_home_row_by_half_a_key()
    {
        var layout = KeyboardLayouts.Full(FullKeyboardLayoutKind.PhoneHybrid);
        var homeRow = layout.Rows[1];

        Assert.Equal(11, homeRow.Keys.Count);
        Assert.Equal(0.5, homeRow.Keys[0].WidthUnits);
        Assert.Equal(KeyStyle.Spacer, homeRow.Keys[0].Style);
        Assert.Equal("key.char.0061", homeRow.Keys[1].Id);
        Assert.Equal(9, homeRow.Keys.Count(key => key.Command.Kind == KeyCommandKind.InsertText));
        Assert.Equal(0.5, homeRow.Keys[10].WidthUnits);
        Assert.Equal(KeyStyle.Spacer, homeRow.Keys[10].Style);

        var shiftRow = layout.Rows[2];
        Assert.Equal(1.5, shiftRow.Keys[0].WidthUnits);
        Assert.Equal(KeyGlyph.Shift, shiftRow.Keys[0].Glyph);
        Assert.Equal(1.5, shiftRow.Keys[^1].WidthUnits);
        Assert.Equal(KeyGlyph.Backspace, shiftRow.Keys[^1].Glyph);
    }

    [Fact]
    public void Phone_hybrid_bottom_row_expands_space_and_has_no_ime_key()
    {
        var keys = KeyboardLayouts.Full(FullKeyboardLayoutKind.PhoneHybrid).Rows[3].Keys;

        Assert.Equal(7, keys.Count);
        Assert.Equal(KeyCommandKind.SwitchToSymbolPage, keys[0].Command.Kind);
        Assert.Equal(1.0, keys[0].WidthUnits);
        Assert.Equal(KeyCommandKind.SwitchToNumericKeypad, keys[1].Command.Kind);
        Assert.Equal(1.0, keys[1].WidthUnits);
        Assert.Equal(KeyGlyph.ArrowLeft, keys[2].Glyph);
        Assert.Equal(1.0, keys[2].WidthUnits);
        Assert.Equal(KeyGlyph.Space, keys[3].Glyph);
        Assert.Equal(3.5, keys[3].WidthUnits);
        Assert.Equal(KeyGlyph.ArrowRight, keys[4].Glyph);
        Assert.Equal(1.0, keys[4].WidthUnits);
        Assert.Equal(KeyGlyph.Clear, keys[5].Glyph);
        Assert.Equal(1.0, keys[5].WidthUnits);
        Assert.Equal(KeyCommandKind.Submit, keys[6].Command.Kind);
        Assert.Equal(1.5, keys[6].WidthUnits);
        Assert.DoesNotContain(keys, key => key.Style == KeyStyle.Spacer);
    }

    [Fact]
    public void Phone_hybrid_aligns_z_with_s()
    {
        var layout = KeyboardLayouts.Full(FullKeyboardLayoutKind.PhoneHybrid);
        var homeKeys = layout.Rows[1].Keys;
        var shiftKeys = layout.Rows[2].Keys;

        // S starts after the half-key inset plus A; Z starts after the 1.5-unit Shift.
        Assert.Equal(WidthBefore(homeKeys, "key.char.0073"), WidthBefore(shiftKeys, "key.char.007a"));
        Assert.Equal(1.5, WidthBefore(homeKeys, "key.char.0073"));
        Assert.Equal(1.5, WidthBefore(shiftKeys, "key.char.007a"));
    }

    [Fact]
    public void Phone_hybrid_puts_the_symbols_entry_left_of_the_numeric_entry()
    {
        var keys = KeyboardLayouts.Full(FullKeyboardLayoutKind.PhoneHybrid).Rows[3].Keys;

        var symbolIndex = keys.ToList().FindIndex(key => key.Command.Kind == KeyCommandKind.SwitchToSymbolPage);
        var numericIndex = keys.ToList().FindIndex(key => key.Command.Kind == KeyCommandKind.SwitchToNumericKeypad);

        Assert.True(symbolIndex >= 0);
        Assert.True(numericIndex >= 0);
        Assert.True(symbolIndex < numericIndex);
        Assert.Equal("key.symbols", keys[symbolIndex].Label.ResourceKey);
        Assert.Null(keys[symbolIndex].Glyph);
        Assert.Null(keys[numericIndex].Glyph);
    }

    [Fact]
    public void Phone_hybrid_uses_icons_for_shift_backspace_clear_and_caret()
    {
        var keys = Flatten(KeyboardLayouts.Full(FullKeyboardLayoutKind.PhoneHybrid)).ToList();

        AssertGlyphKey(keys, "key.shift.left", KeyGlyph.Shift, KeyCommandKind.ToggleShift, resourceKey: "key.shift");
        AssertGlyphKey(keys, "key.backspace", KeyGlyph.Backspace, KeyCommandKind.Backspace, repeatable: true);
        AssertGlyphKey(keys, "key.clear", KeyGlyph.Clear, KeyCommandKind.Clear);
        AssertGlyphKey(keys, "key.caret.left", KeyGlyph.ArrowLeft, KeyCommandKind.MoveCaretLeft, repeatable: true);
        AssertGlyphKey(keys, "key.caret.right", KeyGlyph.ArrowRight, KeyCommandKind.MoveCaretRight, repeatable: true);
    }

    [Fact]
    public void Symbol_page_is_a_four_by_eight_ansi_grid_plus_a_function_row()
    {
        var layout = KeyboardLayouts.Full(FullKeyboardLayoutKind.PhoneHybrid, FullKeyboardPage.Symbols);

        Assert.Equal("full-symbol-page", layout.Id);
        Assert.Equal(5, layout.Rows.Count);
        Assert.All(layout.Rows, row => Assert.Equal(8d, row.TotalUnits));
        Assert.All(layout.Rows.Take(4), row => Assert.Equal(8, row.Keys.Count));
        Assert.Equal(8, layout.Rows[4].Keys.Count);

        var inserts = Flatten(layout)
            .Where(key => key.Command.Kind == KeyCommandKind.InsertText)
            .Select(key => key.Command.Text!)
            .Where(text => text != " ")
            .ToList();

        const string expected = "`~!@#$%^&*()-_=+[]{}\\|;:'\"<>,./?";
        Assert.Equal(expected.ToCharArray().Select(c => c.ToString()), inserts);
        Assert.Equal(32, inserts.Count);
        Assert.Equal(new[] { "`", "~" }, inserts.Take(2));
        Assert.Equal(new[] { "'", "\"", "<", ">", ",", ".", "/", "?" }, inserts.TakeLast(8));
    }

    [Fact]
    public void Symbol_page_function_row_adds_numeric_and_clear_around_the_existing_keys()
    {
        var keys = KeyboardLayouts.Full(FullKeyboardLayoutKind.PhoneHybrid, FullKeyboardPage.Symbols).Rows[4].Keys;

        Assert.Equal(8, keys.Count);
        Assert.All(keys, key => Assert.Equal(1.0, key.WidthUnits));
        Assert.Equal(KeyCommandKind.SwitchToMainPage, keys[0].Command.Kind);
        Assert.Equal("key.letters", keys[0].Label.ResourceKey);
        Assert.Equal(KeyCommandKind.SwitchToNumericKeypad, keys[1].Command.Kind);
        Assert.Equal("key.numbers", keys[1].Label.ResourceKey);
        Assert.Equal(KeyGlyph.ArrowLeft, keys[2].Glyph);
        Assert.Equal(KeyCommand.Insert(" "), keys[3].Command);
        Assert.Equal(KeyGlyph.Space, keys[3].Glyph);
        Assert.Equal(KeyGlyph.ArrowRight, keys[4].Glyph);
        Assert.Equal(KeyCommandKind.Clear, keys[5].Command.Kind);
        Assert.Equal(KeyGlyph.Clear, keys[5].Glyph);
        Assert.Equal(KeyGlyph.Backspace, keys[6].Glyph);
        Assert.True(keys[6].Repeatable);
        Assert.Equal(KeyCommandKind.Submit, keys[7].Command.Kind);
        Assert.Equal(KeyStyle.Primary, keys[7].Style);
    }

    [Fact]
    public void Only_phone_hybrid_has_a_separate_symbol_page()
    {
        Assert.True(KeyboardLayouts.HasSymbolPage(FullKeyboardLayoutKind.PhoneHybrid));
        Assert.False(KeyboardLayouts.HasSymbolPage(FullKeyboardLayoutKind.AnsiStyle));

        // AnsiStyle keeps the ANSI symbols on its main page; the page argument is ignored.
        var ansi = KeyboardLayouts.Full(FullKeyboardLayoutKind.AnsiStyle, FullKeyboardPage.Symbols);
        Assert.Equal("full-ansi-style", ansi.Id);
    }

    [Fact]
    public void General_numpad_is_a_five_by_four_grid_with_the_expected_cells()
    {
        var layout = KeyboardLayouts.GeneralNumeric(NumericKeypadLayoutKind.Numpad);

        Assert.Equal("numeric-general-numpad", layout.Id);
        Assert.Equal(KeyboardMode.NumericKeypad, layout.Mode);
        Assert.True(layout.IsGrid);
        Assert.Empty(layout.Rows);
        Assert.Equal(5, layout.Grid!.Columns);
        Assert.Equal(4, layout.Grid.Rows);

        // Digits sit in a 3x4 block in columns 1..3.
        Assert.Equal("7", At(layout, 0, 1).Command.Text);
        Assert.Equal("8", At(layout, 0, 2).Command.Text);
        Assert.Equal("9", At(layout, 0, 3).Command.Text);
        Assert.Equal("4", At(layout, 1, 1).Command.Text);
        Assert.Equal("5", At(layout, 1, 2).Command.Text);
        Assert.Equal("6", At(layout, 1, 3).Command.Text);
        Assert.Equal("1", At(layout, 2, 1).Command.Text);
        Assert.Equal("2", At(layout, 2, 2).Command.Text);
        Assert.Equal("3", At(layout, 2, 3).Command.Text);

        // Right arrow under 1, zero under 2 and the decimal point under 3.
        Assert.Equal(KeyCommandKind.MoveCaretRight, At(layout, 3, 1).Command.Kind);
        Assert.Equal("0", At(layout, 3, 2).Command.Text);
        Assert.Equal(".", At(layout, 3, 3).Command.Text);
        Assert.Equal(1, CellAt(layout, 3, 2).ColumnSpan);

        // The general surface inserts +/- instead of stepping and offers ABC.
        Assert.Null(At(layout, 0, 0).Glyph);
        Assert.Equal(KeyCommand.Insert("+"), At(layout, 0, 0).Command);
        Assert.Null(At(layout, 1, 0).Glyph);
        Assert.Equal(KeyCommand.Insert("-"), At(layout, 1, 0).Command);
        Assert.Equal(KeyCommandKind.SwitchToFullKeyboard, At(layout, 2, 0).Command.Kind);

        // Backspace, clear and caret left are icons on their fixed cells.
        Assert.Equal(KeyGlyph.Backspace, At(layout, 0, 4).Glyph);
        Assert.True(At(layout, 0, 4).Repeatable);
        Assert.Equal(KeyGlyph.Clear, At(layout, 1, 4).Glyph);
        Assert.Equal(KeyGlyph.ArrowLeft, At(layout, 3, 0).Glyph);

        // An empty right row-2 slot stretches Confirm over both right-hand cells.
        var confirm = CellAt(layout, 2, 4);
        Assert.Equal(1, confirm.ColumnSpan);
        Assert.Equal(2, confirm.RowSpan);
        Assert.Equal(KeyCommandKind.Submit, confirm.Key.Command.Kind);
        Assert.Equal(KeyStyle.Primary, confirm.Key.Style);
        Assert.DoesNotContain(layout.Grid.Cells, cell => cell.Row == 3 && cell.Column == 4);
    }

    [Fact]
    public void Positive_integer_numpad_uses_step_keys_and_grows_zero_and_confirm()
    {
        var layout = KeyboardLayouts.ProfileNumeric(InputProfiles.PositiveInteger);

        Assert.Equal("numeric-profile-positive-integer", layout.Id);
        Assert.Equal(5, layout.Grid!.Columns);
        Assert.Equal(4, layout.Grid.Rows);

        AssertGlyphKey(Flatten(layout).ToList(), "key.step.up", KeyGlyph.ArrowUp, KeyCommandKind.StepDigitUp, repeatable: true);
        AssertGlyphKey(Flatten(layout).ToList(), "key.step.down", KeyGlyph.ArrowDown, KeyCommandKind.StepDigitDown, repeatable: true);

        // No left row-2 key for a plain positive integer.
        Assert.DoesNotContain(layout.Grid.Cells, cell => cell.Row == 2 && cell.Column == 0);

        // The decimal slot is empty, so 0 widens over two columns.
        var zero = CellAt(layout, 3, 2);
        Assert.Equal(2, zero.ColumnSpan);
        Assert.Equal("0", zero.Key.Command.Text);

        // The right row-2 slot is empty, so Confirm stretches over two rows.
        var confirm = CellAt(layout, 2, 4);
        Assert.Equal(1, confirm.ColumnSpan);
        Assert.Equal(2, confirm.RowSpan);

        // No separator key and no sign key.
        Assert.DoesNotContain(Flatten(layout), key => key.Command == KeyCommand.Insert("."));
        Assert.DoesNotContain(Flatten(layout), key => key.Command.Kind == KeyCommandKind.ToggleSign);
    }

    [Fact]
    public void Signed_integer_numpad_puts_the_sign_in_the_left_row2_slot()
    {
        var layout = KeyboardLayouts.ProfileNumeric(InputProfiles.Integer);

        Assert.Equal(KeyCommandKind.ToggleSign, At(layout, 2, 0).Command.Kind);
        Assert.Equal("key.sign", At(layout, 2, 0).Label.ResourceKey);
        Assert.Equal(KeyGlyph.PlusMinus, At(layout, 2, 0).Glyph);

        // The sign only affects the left column: Confirm still grows and 0 still widens.
        Assert.Equal(2, CellAt(layout, 2, 4).RowSpan);
        Assert.Equal(2, CellAt(layout, 3, 2).ColumnSpan);
    }

    [Fact]
    public void Decimal_numpads_add_the_decimal_point_and_sign_only_when_allowed()
    {
        var positive = KeyboardLayouts.ProfileNumeric(InputProfiles.PositiveDecimal);
        Assert.Equal(".", At(positive, 3, 3).Command.Text);
        Assert.Equal(1, CellAt(positive, 3, 2).ColumnSpan);
        Assert.DoesNotContain(Flatten(positive), key => key.Command.Kind == KeyCommandKind.ToggleSign);
        Assert.DoesNotContain(positive.Grid!.Cells, cell => cell.Row == 2 && cell.Column == 0);

        var signed = KeyboardLayouts.ProfileNumeric(InputProfiles.Decimal);
        Assert.Equal(".", At(signed, 3, 3).Command.Text);
        Assert.Equal(KeyCommandKind.ToggleSign, At(signed, 2, 0).Command.Kind);
    }

    [Fact]
    public void Date_and_ip_numpads_use_the_scenario_separator()
    {
        var slash = KeyboardLayouts.ProfileNumeric(InputProfiles.DateSlash);
        var dotDate = KeyboardLayouts.ProfileNumeric(InputProfiles.DateDot);
        var ip = KeyboardLayouts.ProfileNumeric(InputProfiles.IpAddress);

        Assert.Equal("/", At(slash, 3, 3).Command.Text);
        Assert.Equal(".", At(dotDate, 3, 3).Command.Text);
        Assert.Equal(".", At(ip, 3, 3).Command.Text);

        Assert.DoesNotContain(Flatten(slash), key => key.Command == KeyCommand.Insert("."));
        Assert.DoesNotContain(Flatten(slash), key => key.Command.Kind == KeyCommandKind.ToggleSign);
        Assert.DoesNotContain(Flatten(ip), key => key.Command.Kind == KeyCommandKind.ToggleSign);
        Assert.All(new[] { slash, dotDate, ip }, layout =>
        {
            Assert.Equal(KeyGlyph.ArrowUp, At(layout, 0, 0).Glyph);
            Assert.Equal(1, CellAt(layout, 3, 2).ColumnSpan);
            Assert.Equal(2, CellAt(layout, 2, 4).RowSpan);
        });
    }

    [Fact]
    public void A_numpad_with_a_right_row2_key_keeps_confirm_to_one_cell()
    {
        var slots = new NumpadSlots(showStepKeys: true, rightRow2: NumpadSlotKey.Sign);
        var layout = KeyboardLayouts.Numpad("numeric-test-slots", slots);

        Assert.Equal("numeric-test-slots", layout.Id);
        Assert.Equal(KeyCommandKind.ToggleSign, At(layout, 2, 4).Command.Kind);
        Assert.Equal(1, CellAt(layout, 3, 4).RowSpan);
        Assert.Equal(KeyCommandKind.Submit, At(layout, 3, 4).Command.Kind);
    }

    [Fact]
    public void Numpad_slots_derive_from_the_numeric_options()
    {
        var general = NumpadSlots.General;
        Assert.False(general.ShowStepKeys);
        Assert.Equal(NumpadSlotKey.FullKeyboard, general.LeftRow2);
        Assert.Equal(NumpadSlotKey.None, general.RightRow2);
        Assert.Equal((char?)'.', general.Separator);

        var integer = NumpadSlots.From(InputProfiles.Integer.NumericKeypad);
        Assert.True(integer.ShowStepKeys);
        Assert.Equal(NumpadSlotKey.Sign, integer.LeftRow2);
        Assert.Null(integer.Separator);

        var positive = NumpadSlots.From(InputProfiles.PositiveInteger.NumericKeypad);
        Assert.Equal(NumpadSlotKey.None, positive.LeftRow2);
        Assert.Null(positive.Separator);

        var date = NumpadSlots.From(InputProfiles.DateSlash.NumericKeypad);
        Assert.Equal((char?)'/', date.Separator);
    }

    [Fact]
    public void Sequence_is_retained_as_the_phone_style_six_column_surface()
    {
        var layout = KeyboardLayouts.GeneralNumeric(NumericKeypadLayoutKind.Sequence);

        Assert.Equal("numeric-general-sequence", layout.Id);
        Assert.Equal(KeyboardMode.NumericKeypad, layout.Mode);
        Assert.False(layout.IsGrid);
        Assert.Equal(5, layout.Rows.Count);
        Assert.All(layout.Rows, row => Assert.Equal(6d, row.TotalUnits));

        var keys = Flatten(layout).ToList();
        Assert.Contains(keys, key => key.Command.Kind == KeyCommandKind.SwitchToFullKeyboard);
        Assert.Contains(keys, key => key.Command.Kind == KeyCommandKind.Submit);
        Assert.Contains(keys, key => key.Command == KeyCommand.Insert("$"));
        Assert.Contains(keys, key => key.Command == KeyCommand.Insert("?"));
        Assert.DoesNotContain(keys, key => key.Command.Kind == KeyCommandKind.Clear);
    }

    [Fact]
    public void The_numpad_never_offers_the_symbol_page()
    {
        var layouts = AllLayouts().Where(layout => layout.Mode == KeyboardMode.NumericKeypad).ToList();
        Assert.NotEmpty(layouts);
        Assert.All(
            layouts,
            layout => Assert.DoesNotContain(
                Flatten(layout),
                key => key.Command.Kind == KeyCommandKind.SwitchToSymbolPage));
    }

    [Fact]
    public void No_built_in_profile_uses_the_sequence_surface()
    {
        foreach (var profile in InputProfiles.All.Where(profile => !profile.AllowsFullKeyboard))
        {
            var layout = KeyboardLayouts.ProfileNumeric(profile);
            Assert.NotEqual("numeric-general-sequence", layout.Id);
            Assert.DoesNotContain("sequence", layout.Id);
            Assert.True(layout.IsGrid);
        }
    }

    [Fact]
    public void Every_layout_has_unique_key_ids()
    {
        foreach (var layout in AllLayouts())
        {
            var ids = Flatten(layout).Select(key => key.Id).ToList();
            Assert.Equal(ids.Count, ids.Distinct(StringComparer.Ordinal).Count());
        }
    }

    [Fact]
    public void Text_keys_never_carry_a_glyph_and_icon_keys_always_carry_a_label()
    {
        var keys = AllLayouts().SelectMany(Flatten).ToList();

        Assert.All(keys.Where(key => key.Glyph is not null), key => Assert.True(key.Label.IsResource));
        Assert.All(
            keys.Where(key => key.Id.StartsWith("key.char.", StringComparison.Ordinal)),
            key =>
            {
                Assert.Null(key.Glyph);
                Assert.False(key.Label.IsResource);
                Assert.Equal(key.Command.Text, key.Label.Text);
            });
    }

    [Fact]
    public void Profiled_numeric_rejects_a_full_keyboard_profile()
        => Assert.Throws<ArgumentException>(
            () => KeyboardLayouts.ProfileNumeric(InputProfiles.Text));

    [Fact]
    public void Grid_layout_rejects_overlapping_and_out_of_bounds_cells()
    {
        var key = new KeyDefinition("key.a", KeyLabel.Literal("a"), KeyCommand.Insert("a"));
        var overlapping = new KeyDefinition("key.b", KeyLabel.Literal("b"), KeyCommand.Insert("b"), columnSpan: 2);

        Assert.Throws<ArgumentException>(() => new KeyboardGridLayout(
            2,
            1,
            new[] { new KeyboardGridCell(0, 0, key), new KeyboardGridCell(0, 0, key) }));

        Assert.Throws<ArgumentException>(() => new KeyboardGridLayout(
            2,
            1,
            new[] { new KeyboardGridCell(0, 1, overlapping) }));

        Assert.Throws<ArgumentOutOfRangeException>(() => new KeyboardGridLayout(
            0,
            1,
            new[] { new KeyboardGridCell(0, 0, key) }));

        Assert.Throws<ArgumentOutOfRangeException>(() => new KeyboardGridCell(-1, 0, key));
    }

    private static double WidthBefore(IReadOnlyList<KeyDefinition> keys, string id)
    {
        double total = 0;
        foreach (var key in keys)
        {
            if (key.Id == id)
            {
                return total;
            }

            total += key.WidthUnits;
        }

        throw new InvalidOperationException($"Key '{id}' not found.");
    }

    private static void AssertGlyphKey(
        IEnumerable<KeyDefinition> keys,
        string id,
        KeyGlyph glyph,
        KeyCommandKind kind,
        bool repeatable = false,
        string? resourceKey = null)
    {
        var key = keys.Single(candidate => candidate.Id == id);
        Assert.Equal(glyph, key.Glyph);
        Assert.Equal(kind, key.Command.Kind);
        Assert.Equal(repeatable, key.Repeatable);
        Assert.True(key.Label.IsResource);
        Assert.Equal(resourceKey ?? id, key.Label.ResourceKey);
    }
}
