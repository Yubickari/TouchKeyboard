using TouchKeyboard.Core;
using Xunit;

namespace TouchKeyboard.Core.Tests;

public sealed class KeyboardSessionTests
{
    private static KeyboardSession NewSession(
        InputProfile profile,
        string text = "",
        int maxLength = 0,
        decimal? minimum = null,
        decimal? maximum = null,
        bool allowsNegative = true,
        FullKeyboardLayoutKind fullKeyboardLayout = FullKeyboardLayoutKind.AnsiStyle,
        NumericKeypadLayoutKind numericLayout = NumericKeypadLayoutKind.Numpad)
        => new KeyboardSession(new KeyboardSessionOptions
        {
            Text = text,
            Profile = profile,
            MaxLength = maxLength,
            Minimum = minimum,
            Maximum = maximum,
            AllowsNegativeSign = allowsNegative,
            FullKeyboardLayout = fullKeyboardLayout,
            NumericKeypadLayout = numericLayout
        });

    private static string Commit(KeyboardSession session)
    {
        Assert.True(session.TryCommit(out var text, out var error), error?.ToString());
        Assert.Null(error);
        return text;
    }

    [Fact]
    public void Initial_state_uses_the_profile_default_mode_and_tail_caret()
    {
        var text = NewSession(InputProfiles.Text, "abc");
        Assert.Equal(KeyboardMode.FullKeyboard, text.State.Mode);
        Assert.Equal("full-ansi-style", text.State.Layout.Id);
        Assert.Equal(FullKeyboardPage.Main, text.State.Page);
        Assert.Equal(3, text.State.CaretIndex);
        Assert.False(text.State.IsDirty);

        var phone = NewSession(InputProfiles.Text, "abc", fullKeyboardLayout: FullKeyboardLayoutKind.PhoneHybrid);
        Assert.Equal("full-phone-hybrid", phone.State.Layout.Id);

        var number = NewSession(InputProfiles.PositiveInteger, "12");
        Assert.Equal(KeyboardMode.NumericKeypad, number.State.Mode);
        Assert.Equal("numeric-profile-positive-integer", number.State.Layout.Id);
        Assert.True(number.State.Layout.IsGrid);
        Assert.Equal(FullKeyboardPage.Main, number.State.Page);
        Assert.Equal(2, number.State.CaretIndex);
    }

    [Fact]
    public void Insert_backspace_and_clear_update_text_and_caret()
    {
        var session = NewSession(InputProfiles.Text);

        session.Execute(KeyCommand.Insert("a"));
        session.Execute(KeyCommand.Insert("b"));
        Assert.Equal("ab", session.State.Text);
        Assert.Equal(2, session.State.CaretIndex);

        session.Execute(KeyCommand.Backspace);
        Assert.Equal("a", session.State.Text);
        Assert.Equal(1, session.State.CaretIndex);

        session.Execute(KeyCommand.Clear);
        Assert.Equal(string.Empty, session.State.Text);
        Assert.Equal(0, session.State.CaretIndex);
        Assert.Null(session.State.Error);
    }

    [Fact]
    public void Caret_moves_and_clamps_at_both_ends()
    {
        var session = NewSession(InputProfiles.Text, "abc");

        session.Execute(KeyCommand.MoveCaretLeft);
        Assert.Equal(2, session.State.CaretIndex);
        session.Execute(KeyCommand.MoveCaretLeft);
        session.Execute(KeyCommand.MoveCaretLeft);
        session.Execute(KeyCommand.MoveCaretLeft);
        Assert.Equal(0, session.State.CaretIndex);

        session.Execute(KeyCommand.MoveCaretRight);
        Assert.Equal(1, session.State.CaretIndex);
    }

    [Fact]
    public void Max_length_rejects_extra_characters()
    {
        var session = NewSession(InputProfiles.Text, maxLength: 3);

        session.Execute(KeyCommand.Insert("a"));
        session.Execute(KeyCommand.Insert("b"));
        session.Execute(KeyCommand.Insert("c"));
        session.Execute(KeyCommand.Insert("d"));

        Assert.Equal("abc", session.State.Text);
    }

    [Fact]
    public void Switching_between_full_and_general_numeric_preserves_text_and_caret()
    {
        var session = NewSession(InputProfiles.Text, "12");
        session.Execute(KeyCommand.MoveCaretLeft);

        session.Execute(KeyCommand.SwitchToNumericKeypad);
        Assert.Equal(KeyboardMode.NumericKeypad, session.State.Mode);
        Assert.Equal("numeric-general-numpad", session.State.Layout.Id);
        Assert.True(session.State.Layout.IsGrid);
        Assert.Equal("12", session.State.Text);
        Assert.Equal(1, session.State.CaretIndex);

        session.SetNumericKeypadLayout(NumericKeypadLayoutKind.Sequence);
        Assert.Equal("numeric-general-sequence", session.State.Layout.Id);
        Assert.False(session.State.Layout.IsGrid);

        session.Execute(KeyCommand.SwitchToFullKeyboard);
        Assert.Equal(KeyboardMode.FullKeyboard, session.State.Mode);
        Assert.Equal("full-ansi-style", session.State.Layout.Id);
        Assert.Equal("12", session.State.Text);
    }

    [Fact]
    public void Shift_toggles_in_full_mode_and_resets_on_mode_switch()
    {
        var session = NewSession(InputProfiles.Text);

        session.Execute(KeyCommand.ToggleShift);
        Assert.True(session.State.IsShiftActive);

        session.Execute(KeyCommand.SwitchToNumericKeypad);
        Assert.False(session.State.IsShiftActive);

        session.Execute(KeyCommand.SwitchToFullKeyboard);
        Assert.False(session.State.IsShiftActive);
    }

    [Fact]
    public void Restricted_numeric_profile_locks_mode_and_layout()
    {
        var session = NewSession(InputProfiles.PositiveInteger);

        Assert.Equal(KeyboardMode.NumericKeypad, session.State.Mode);
        session.Execute(KeyCommand.SwitchToFullKeyboard);
        Assert.Equal(KeyboardMode.NumericKeypad, session.State.Mode);
        Assert.Equal("numeric-profile-positive-integer", session.State.Layout.Id);

        session.SetNumericKeypadLayout(NumericKeypadLayoutKind.Sequence);
        Assert.Equal("numeric-profile-positive-integer", session.State.Layout.Id);
    }

    [Fact]
    public void Symbol_page_switches_only_for_the_phone_hybrid_geometry()
    {
        var phone = NewSession(InputProfiles.Text, fullKeyboardLayout: FullKeyboardLayoutKind.PhoneHybrid);

        phone.Execute(KeyCommand.SwitchToSymbolPage);
        Assert.Equal(KeyboardMode.FullKeyboard, phone.State.Mode);
        Assert.Equal(FullKeyboardPage.Symbols, phone.State.Page);
        Assert.Equal("full-symbol-page", phone.State.Layout.Id);

        phone.Execute(KeyCommand.SwitchToMainPage);
        Assert.Equal(FullKeyboardPage.Main, phone.State.Page);
        Assert.Equal("full-phone-hybrid", phone.State.Layout.Id);

        var ansi = NewSession(InputProfiles.Text);
        ansi.Execute(KeyCommand.SwitchToSymbolPage);
        Assert.Equal(FullKeyboardPage.Main, ansi.State.Page);
        Assert.Equal("full-ansi-style", ansi.State.Layout.Id);

        ansi.Execute(KeyCommand.SwitchToMainPage);
        Assert.Equal(FullKeyboardPage.Main, ansi.State.Page);
    }

    [Fact]
    public void Switching_pages_resets_shift()
    {
        var session = NewSession(InputProfiles.Text, fullKeyboardLayout: FullKeyboardLayoutKind.PhoneHybrid);
        session.Execute(KeyCommand.ToggleShift);
        Assert.True(session.State.IsShiftActive);

        session.Execute(KeyCommand.SwitchToSymbolPage);
        Assert.False(session.State.IsShiftActive);
    }

    [Fact]
    public void Numeric_switch_always_returns_to_the_main_page()
    {
        var session = NewSession(InputProfiles.Text, fullKeyboardLayout: FullKeyboardLayoutKind.PhoneHybrid);
        session.Execute(KeyCommand.SwitchToSymbolPage);
        Assert.Equal(FullKeyboardPage.Symbols, session.State.Page);

        session.Execute(KeyCommand.SwitchToNumericKeypad);
        Assert.Equal(KeyboardMode.NumericKeypad, session.State.Mode);
        Assert.Equal(FullKeyboardPage.Main, session.State.Page);
        Assert.Equal("numeric-general-numpad", session.State.Layout.Id);

        session.Execute(KeyCommand.SwitchToFullKeyboard);
        Assert.Equal(FullKeyboardPage.Main, session.State.Page);
        Assert.Equal("full-phone-hybrid", session.State.Layout.Id);
    }

    [Fact]
    public void The_numpad_never_enters_the_symbol_page()
    {
        var restricted = NewSession(InputProfiles.PositiveInteger);
        restricted.Execute(KeyCommand.SwitchToSymbolPage);
        Assert.Equal(FullKeyboardPage.Main, restricted.State.Page);
        Assert.Equal("numeric-profile-positive-integer", restricted.State.Layout.Id);

        var general = NewSession(InputProfiles.Text);
        general.Execute(KeyCommand.SwitchToNumericKeypad);
        general.Execute(KeyCommand.SwitchToSymbolPage);
        Assert.Equal(FullKeyboardPage.Main, general.State.Page);
        Assert.Equal("numeric-general-numpad", general.State.Layout.Id);
    }

    [Fact]
    public void Ansi_style_session_builds_the_strict_layout()
    {
        var session = NewSession(InputProfiles.Text, fullKeyboardLayout: FullKeyboardLayoutKind.AnsiStyle);

        Assert.Equal("full-ansi-style", session.State.Layout.Id);
        Assert.Equal(5, session.State.Layout.Rows.Count);
        Assert.False(session.State.Layout.IsGrid);
    }

    [Fact]
    public void Step_digit_wraps_and_ignores_non_digits()
    {
        var session = NewSession(InputProfiles.Integer, "9");
        session.Execute(KeyCommand.StepDigitUp);
        Assert.Equal("0", session.State.Text);
        session.Execute(KeyCommand.StepDigitDown);
        Assert.Equal("9", session.State.Text);

        session.Execute(KeyCommand.Clear);
        session.Execute(KeyCommand.Insert("12"));
        session.Execute(KeyCommand.StepDigitDown);
        Assert.Equal("11", session.State.Text);

        session.Execute(KeyCommand.MoveCaretLeft);
        session.Execute(KeyCommand.StepDigitUp);
        Assert.Equal("21", session.State.Text);

        session.Execute(KeyCommand.MoveCaretLeft);
        session.Execute(KeyCommand.StepDigitUp);
        Assert.Equal("21", session.State.Text);
    }

    [Fact]
    public void Step_digit_is_inactive_in_the_full_keyboard()
    {
        var session = NewSession(InputProfiles.Text, "9");
        session.Execute(KeyCommand.StepDigitUp);
        Assert.Equal("9", session.State.Text);
    }

    [Fact]
    public void Toggle_sign_adds_and_removes_a_leading_minus_and_keeps_the_caret()
    {
        var session = NewSession(InputProfiles.Integer, "5");
        Assert.Equal(1, session.State.CaretIndex);

        session.Execute(KeyCommand.ToggleSign);
        Assert.Equal("-5", session.State.Text);
        Assert.Equal(2, session.State.CaretIndex);

        session.Execute(KeyCommand.ToggleSign);
        Assert.Equal("5", session.State.Text);
        Assert.Equal(1, session.State.CaretIndex);
    }

    [Fact]
    public void Toggle_sign_is_ignored_for_positive_profiles_and_when_negative_is_disallowed()
    {
        var positive = NewSession(InputProfiles.PositiveInteger, "5");
        positive.Execute(KeyCommand.ToggleSign);
        Assert.Equal("5", positive.State.Text);

        var disallowed = NewSession(InputProfiles.Integer, "5", allowsNegative: false);
        disallowed.Execute(KeyCommand.ToggleSign);
        Assert.Equal("5", disallowed.State.Text);
    }

    [Fact]
    public void Commit_normalizes_per_profile()
    {
        Assert.Equal("0.5", Commit(SessionWithText(InputProfiles.Decimal, ".5")));
        Assert.Equal("007", Commit(SessionWithText(InputProfiles.Integer, "007")));
        Assert.Equal("2026/02/03", Commit(SessionWithText(InputProfiles.DateSlash, "2026/2/3")));
        Assert.Equal("192.168.1.10", Commit(SessionWithText(InputProfiles.IpAddress, "192.168.001.010")));
        Assert.Equal("abc", Commit(SessionWithText(InputProfiles.Text, "abc")));
    }

    [Fact]
    public void Commit_rejects_empty_and_out_of_range_values()
    {
        var empty = NewSession(InputProfiles.PositiveInteger);
        Assert.False(empty.TryCommit(out _, out var error));
        Assert.Equal(InputValidationErrorCode.EmptyNotAllowed, error!.Code);

        var tooLarge = NewSession(InputProfiles.Integer, "11", minimum: 0, maximum: 10);
        Assert.False(tooLarge.TryCommit(out _, out error));
        Assert.Equal(InputValidationErrorCode.ValueTooLarge, error!.Code);

        var tooSmall = NewSession(InputProfiles.Integer, "-1", minimum: 0);
        Assert.False(tooSmall.TryCommit(out _, out error));
        Assert.Equal(InputValidationErrorCode.ValueTooSmall, error!.Code);
    }

    [Fact]
    public void An_edit_clears_the_previous_validation_error()
    {
        var session = NewSession(InputProfiles.PositiveInteger);
        Assert.False(session.TryCommit(out _, out _));
        Assert.NotNull(session.State.Error);

        session.Execute(KeyCommand.Insert("5"));
        Assert.Null(session.State.Error);
        Assert.Equal("5", session.State.Text);
    }

    [Fact]
    public void Cancel_discards_and_blocks_further_edits_and_commit()
    {
        var session = NewSession(InputProfiles.Text, "a");
        session.Execute(KeyCommand.Insert("b"));
        session.Cancel();

        Assert.True(session.IsCancelled);
        Assert.True(session.State.IsCancelled);
        Assert.Equal("ab", session.State.Text);

        session.Execute(KeyCommand.Insert("c"));
        Assert.Equal("ab", session.State.Text);

        Assert.False(session.TryCommit(out _, out var error));
        Assert.Equal(InputValidationErrorCode.CommitRejected, error!.Code);
    }

    [Fact]
    public void Report_commit_failure_sets_the_error_without_closing()
    {
        var session = NewSession(InputProfiles.Text, "abc");
        session.ReportCommitFailure(new InputValidationError(InputValidationErrorCode.CommitRejected));

        Assert.NotNull(session.State.Error);
        Assert.False(session.State.IsCancelled);
    }

    [Fact]
    public void Is_dirty_tracks_changes_from_the_initial_value()
    {
        var session = NewSession(InputProfiles.Text, "a");
        Assert.False(session.State.IsDirty);

        session.Execute(KeyCommand.Insert("b"));
        Assert.True(session.State.IsDirty);

        session.Execute(KeyCommand.Backspace);
        Assert.False(session.State.IsDirty);
    }

    [Fact]
    public void State_changed_fires_for_edits_commit_and_page_switches()
    {
        var session = NewSession(InputProfiles.Text, fullKeyboardLayout: FullKeyboardLayoutKind.PhoneHybrid);
        var changes = 0;
        session.StateChanged += (_, _) => changes++;

        session.Execute(KeyCommand.Insert("a"));
        session.TryCommit(out _, out _);
        var beforePage = changes;
        session.Execute(KeyCommand.SwitchToSymbolPage);

        Assert.True(changes >= 2);
        Assert.Equal(beforePage + 1, changes);
    }

    private static KeyboardSession SessionWithText(InputProfile profile, string text)
        => NewSession(profile, text);
}
