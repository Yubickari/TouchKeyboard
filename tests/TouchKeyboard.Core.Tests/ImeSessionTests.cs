using System;
using System.Collections.Generic;
using System.Linq;
using TouchKeyboard.Core;
using Xunit;

namespace TouchKeyboard.Core.Tests;

public sealed class ImeSessionTests
{
    private sealed class FakeImeEngine : IImeEngine
    {
        public string LanguageTag => "zh-Hans";

        public bool TryQuery(string input, int maxCandidates, out IReadOnlyList<ImeCandidate> candidates)
        {
            var all = input switch
            {
                "nihao" => new[] { "你好", "拟好", "你" },
                "ni" => new[] { "你", "尼", "泥", "拟", "逆", "妮", "倪" },
                "hao" => new[] { "好", "号", "浩" },
                "bj" => new[] { "北京", "背景" },
                "beijing" => new[] { "北京", "背景" },
                "chang" => new[] { "长", "常", "场" },
                "zzz" => Array.Empty<string>(),
                _ => new[] { "候选" + input }
            };

            candidates = all
                .Take(Math.Max(0, maxCandidates))
                .Select(text => new ImeCandidate(text))
                .ToArray();
            return candidates.Count > 0;
        }

        public bool TryMapPunctuation(char input, out string mapped)
        {
            switch (input)
            {
                case ',':
                    mapped = "，";
                    return true;
                case '.':
                    mapped = "。";
                    return true;
                case '?':
                    mapped = "？";
                    return true;
                case '@':
                    mapped = "@";
                    return false;
                default:
                    mapped = string.Empty;
                    return false;
            }
        }
    }

    private static KeyboardSession NewSession(
        InputProfile? profile = null,
        string text = "",
        int maxLength = 0,
        bool? imeEnabled = null,
        int pageSize = 6,
        int maxCompositionLength = 32,
        bool fullWidthPunctuation = true,
        FullKeyboardLayoutKind layout = FullKeyboardLayoutKind.AnsiStyle,
        IImeEngine? engine = null)
        => new KeyboardSession(new KeyboardSessionOptions
        {
            Text = text,
            Profile = profile ?? InputProfiles.Text,
            MaxLength = maxLength,
            FullKeyboardLayout = layout,
            ImeEngine = engine ?? new FakeImeEngine(),
            ImeEnabled = imeEnabled,
            ImeCandidatePageSize = pageSize,
            ImeMaxCompositionLength = maxCompositionLength,
            ImeFullWidthPunctuation = fullWidthPunctuation
        });

    private static KeyboardSession NoEngineSession(
        InputProfile profile,
        FullKeyboardLayoutKind layout = FullKeyboardLayoutKind.AnsiStyle)
        => new KeyboardSession(new KeyboardSessionOptions
        {
            Profile = profile,
            FullKeyboardLayout = layout
        });

    private static KeyDefinition ImeKey(KeyboardSession session)
        => session.State.Layout.Rows.SelectMany(row => row.Keys).Single(key => key.Id == "ime.toggle");

    private static void Type(KeyboardSession session, string letters)
    {
        foreach (var letter in letters)
        {
            session.Execute(KeyCommand.Insert(letter.ToString()));
        }
    }

    [Fact]
    public void Only_the_text_profile_allows_ime()
    {
        Assert.True(InputProfiles.Text.AllowsIme);
        Assert.All(
            InputProfiles.All.Where(profile => profile.Id != "text"),
            profile => Assert.False(profile.AllowsIme));

        var custom = new CustomProfile();
        Assert.False(custom.AllowsIme);
    }

    [Fact]
    public void No_engine_has_no_ime_state_and_expands_space()
    {
        var session = NoEngineSession(InputProfiles.Text);

        Assert.Null(session.State.Ime);
        Assert.DoesNotContain(
            session.State.Layout.Rows.SelectMany(row => row.Keys),
            key => key.Command.Kind == KeyCommandKind.ToggleIme);
        Assert.Equal(7.0, session.State.Layout.Rows[4].Keys.Single(key => key.Id == "key.space").WidthUnits);
        Assert.Equal(15d, session.State.Layout.Rows[4].TotalUnits);
    }

    [Fact]
    public void An_engine_with_an_ime_capable_profile_adds_the_toggle_and_restores_space()
    {
        var ansi = NewSession();
        Assert.NotNull(ansi.State.Ime);
        Assert.True(ansi.State.Ime!.IsEnabled);
        Assert.False(ansi.State.Ime.IsComposing);
        Assert.Equal("key.ime.active", ImeKey(ansi).Label.ResourceKey);
        Assert.Equal("key.ime.active.name", ImeKey(ansi).AccessibleName!.Value.ResourceKey);
        Assert.Equal(1.5, ImeKey(ansi).WidthUnits);
        Assert.Equal(5.5, ansi.State.Layout.Rows[4].Keys.Single(key => key.Id == "key.space").WidthUnits);
        Assert.Equal(15d, ansi.State.Layout.Rows[4].TotalUnits);

        var phone = NewSession(layout: FullKeyboardLayoutKind.PhoneHybrid);
        Assert.Equal(1.0, ImeKey(phone).WidthUnits);
        Assert.Equal(2.5, phone.State.Layout.Rows[3].Keys.Single(key => key.Id == "key.space").WidthUnits);
        Assert.Equal(10d, phone.State.Layout.Rows[3].TotalUnits);
    }

    [Fact]
    public void An_engine_does_not_add_ime_to_ime_incapable_profiles()
    {
        foreach (var profile in InputProfiles.All.Where(profile => !profile.AllowsIme))
        {
            var session = NewSession(profile);
            Assert.Null(session.State.Ime);
            Assert.DoesNotContain(
                session.State.Layout.IsGrid
                    ? session.State.Layout.Grid!.Cells.Select(cell => cell.Key)
                    : session.State.Layout.Rows.SelectMany(row => row.Keys),
                key => key.Command.Kind == KeyCommandKind.ToggleIme);
        }
    }

    [Fact]
    public void A_numeric_only_custom_profile_does_not_get_ime_even_when_it_allows_it()
    {
        var session = NewSession(new NumericImeProfile());

        Assert.Null(session.State.Ime);
        Assert.Equal(KeyboardMode.NumericKeypad, session.State.Mode);
    }

    [Fact]
    public void Toggling_ime_keeps_the_key_and_the_layout_width()
    {
        var session = NewSession();
        var before = session.State.Layout;

        session.Execute(KeyCommand.ToggleIme);

        Assert.False(session.State.Ime!.IsEnabled);
        var key = ImeKey(session);
        Assert.Equal("key.ime.inactive", key.Label.ResourceKey);
        Assert.Equal("key.ime.inactive.name", key.AccessibleName!.Value.ResourceKey);
        Assert.Equal(5.5, session.State.Layout.Rows[4].Keys.Single(candidate => candidate.Id == "key.space").WidthUnits);
        Assert.Equal(15d, session.State.Layout.Rows[4].TotalUnits);
        Assert.Equal(before.Rows[4].TotalUnits, session.State.Layout.Rows[4].TotalUnits);
    }

    [Fact]
    public void Letters_start_and_extend_a_composition_without_touching_the_buffer()
    {
        var session = NewSession();

        Type(session, "nihao");

        Assert.Equal(string.Empty, session.State.Text);
        Assert.False(session.State.IsDirty);
        Assert.True(session.State.Ime!.IsComposing);
        Assert.Equal("nihao", session.State.Ime.Input);
        Assert.Equal("你好", session.State.Ime.Candidates[0].Text);
        Assert.Equal(0, session.State.Ime.SelectedIndex);
    }

    [Fact]
    public void Uppercase_physical_letters_are_lowercased_in_the_composition()
    {
        var session = NewSession();

        session.Execute(KeyCommand.Insert("N"));
        session.Execute(KeyCommand.Insert("I"));

        Assert.Equal("ni", session.State.Ime!.Input);
        Assert.Equal("你", session.State.Ime.Candidates[0].Text);
    }

    [Fact]
    public void Backspace_removes_pinyin_letters_and_ends_the_composition()
    {
        var session = NewSession();
        Type(session, "nihao");

        session.Execute(KeyCommand.Backspace);
        Assert.Equal("niha", session.State.Ime!.Input);

        while (session.State.Ime!.IsComposing)
        {
            session.Execute(KeyCommand.Backspace);
        }

        Assert.Equal(string.Empty, session.State.Ime.Input);
        Assert.Empty(session.State.Ime.Candidates);
        Assert.Equal(string.Empty, session.State.Text);
    }

    [Fact]
    public void Space_commits_the_selected_candidate()
    {
        var session = NewSession();
        Type(session, "nihao");

        session.Execute(KeyCommand.Insert(" "));

        Assert.Equal("你好", session.State.Text);
        Assert.False(session.State.Ime!.IsComposing);
        Assert.True(session.State.IsDirty);
    }

    [Fact]
    public void Arrow_keys_move_the_candidate_selection_across_pages()
    {
        var session = NewSession(pageSize: 3);
        Type(session, "ni");

        Assert.Equal(3, session.State.Ime!.Candidates.Count);
        Assert.Equal(0, session.State.Ime.SelectedIndex);
        Assert.Equal(0, session.State.Ime.PageIndex);
        Assert.Equal(3, session.State.Ime.PageCount);

        session.Execute(KeyCommand.MoveCaretRight);
        Assert.Equal(1, session.State.Ime.SelectedIndex);
        session.Execute(KeyCommand.MoveCaretRight);
        session.Execute(KeyCommand.MoveCaretRight);
        Assert.Equal(1, session.State.Ime.PageIndex);
        Assert.Equal(0, session.State.Ime.SelectedIndex);

        session.Execute(KeyCommand.MoveCaretLeft);
        Assert.Equal(0, session.State.Ime.PageIndex);
        Assert.Equal(2, session.State.Ime.SelectedIndex);
    }

    [Fact]
    public void Up_and_down_move_candidates_while_composing()
    {
        var session = NewSession(pageSize: 3);
        Type(session, "ni");

        session.Execute(KeyCommand.StepDigitDown);
        Assert.Equal(1, session.State.Ime!.SelectedIndex);

        session.Execute(KeyCommand.StepDigitUp);
        Assert.Equal(0, session.State.Ime!.SelectedIndex);
    }

    [Fact]
    public void Number_keys_select_and_commit_a_candidate()
    {
        var session = NewSession();
        Type(session, "nihao");

        session.Execute(KeyCommand.Insert("2"));

        Assert.Equal("拟好", session.State.Text);
        Assert.False(session.State.Ime!.IsComposing);
    }

    [Fact]
    public void Zero_and_out_of_range_numbers_are_ignored_while_composing()
    {
        var session = NewSession();
        Type(session, "nihao");

        session.Execute(KeyCommand.Insert("0"));
        session.Execute(KeyCommand.Insert("9"));

        Assert.True(session.State.Ime!.IsComposing);
        Assert.Equal("nihao", session.State.Ime.Input);
        Assert.Equal(string.Empty, session.State.Text);
    }

    [Fact]
    public void Candidate_page_buttons_move_pages_and_reset_the_selection()
    {
        var session = NewSession(pageSize: 3);
        Type(session, "ni");
        session.Execute(KeyCommand.MoveCaretRight);
        Assert.Equal(1, session.State.Ime!.SelectedIndex);

        session.ChangeImePage(1);
        Assert.Equal(1, session.State.Ime!.PageIndex);
        Assert.Equal(0, session.State.Ime.SelectedIndex);

        session.ChangeImePage(-1);
        Assert.Equal(0, session.State.Ime!.PageIndex);
    }

    [Fact]
    public void Select_candidate_by_index_commits_and_out_of_range_is_ignored()
    {
        var session = NewSession();
        Type(session, "nihao");

        session.SelectImeCandidate(99);
        Assert.True(session.State.Ime!.IsComposing);

        session.SelectImeCandidate(1);
        Assert.Equal("拟好", session.State.Text);
        Assert.False(session.State.Ime!.IsComposing);
    }

    [Fact]
    public void Enter_commits_the_candidate_and_then_the_field()
    {
        var session = NewSession();
        Type(session, "nihao");

        Assert.True(session.TryCommit(out var text, out var error));

        Assert.Null(error);
        Assert.Equal("你好", text);
        Assert.Equal("你好", session.State.Text);
        Assert.False(session.State.Ime!.IsComposing);
    }

    [Fact]
    public void No_candidate_commits_the_raw_pinyin()
    {
        var session = NewSession();
        Type(session, "zzz");

        Assert.True(session.State.Ime!.IsComposing);
        Assert.Empty(session.State.Ime.Candidates);
        Assert.Equal(0, session.State.Ime.PageCount);
        Assert.Equal(-1, session.State.Ime.SelectedIndex);

        session.Execute(KeyCommand.Insert(" "));
        Assert.Equal("zzz", session.State.Text);
        Assert.False(session.State.Ime!.IsComposing);
    }

    [Fact]
    public void Punctuation_maps_full_width_only_while_ime_is_enabled()
    {
        var chinese = NewSession();
        Type(chinese, "nihao");
        chinese.Execute(KeyCommand.Insert(","));
        Assert.Equal("你好，", chinese.State.Text);

        var english = NewSession(imeEnabled: false);
        english.Execute(KeyCommand.Insert(","));
        Assert.Equal(",", english.State.Text);

        var disabledMapping = NewSession(fullWidthPunctuation: false);
        Type(disabledMapping, "nihao");
        disabledMapping.Execute(KeyCommand.Insert("."));
        Assert.Equal("你好.", disabledMapping.State.Text);
    }

    [Fact]
    public void Technical_punctuation_stays_ascii()
    {
        var session = NewSession();
        Type(session, "nihao");
        session.Execute(KeyCommand.Insert("@"));
        Assert.Equal("你好@", session.State.Text);
    }

    [Fact]
    public void Mode_page_and_ime_toggles_commit_the_composition_first()
    {
        var phone = NewSession(layout: FullKeyboardLayoutKind.PhoneHybrid);
        Type(phone, "nihao");
        phone.Execute(KeyCommand.ToggleShift);
        Assert.Equal("你好", phone.State.Text);
        Assert.False(phone.State.Ime!.IsComposing);

        var page = NewSession(layout: FullKeyboardLayoutKind.PhoneHybrid);
        Type(page, "nihao");
        page.Execute(KeyCommand.SwitchToSymbolPage);
        Assert.Equal("你好", page.State.Text);
        Assert.Equal(FullKeyboardPage.Symbols, page.State.Page);
        Assert.False(page.State.Ime!.IsComposing);

        var mode = NewSession();
        Type(mode, "nihao");
        mode.Execute(KeyCommand.SwitchToNumericKeypad);
        Assert.Equal("你好", mode.State.Text);
        Assert.Equal(KeyboardMode.NumericKeypad, mode.State.Mode);

        var ime = NewSession();
        Type(ime, "nihao");
        ime.Execute(KeyCommand.ToggleIme);
        Assert.Equal("你好", ime.State.Text);
        Assert.False(ime.State.Ime!.IsEnabled);
        Assert.False(ime.State.Ime.IsComposing);
    }

    [Fact]
    public void Clear_clears_both_the_composition_and_the_buffer()
    {
        var session = NewSession(text: "abc");
        Type(session, "ni");

        session.Execute(KeyCommand.Clear);

        Assert.Equal(string.Empty, session.State.Text);
        Assert.False(session.State.Ime!.IsComposing);
        Assert.Empty(session.State.Ime.Candidates);
    }

    [Fact]
    public void Cancel_discards_the_composition_without_writing_back()
    {
        var session = NewSession();
        Type(session, "nihao");

        session.Cancel();

        Assert.True(session.IsCancelled);
        Assert.Equal(string.Empty, session.State.Text);
        Assert.False(session.State.Ime!.IsComposing);
        Assert.False(session.TryCommit(out _, out var error));
        Assert.Equal(InputValidationErrorCode.CommitRejected, error!.Code);
    }

    [Fact]
    public void Candidate_commit_that_exceeds_max_length_keeps_the_composition_and_reports_it()
    {
        var session = NewSession(maxLength: 1);
        Type(session, "nihao");

        Assert.False(session.TryCommit(out _, out var error));

        Assert.Equal(InputValidationErrorCode.MaxLengthExceeded, error!.Code);
        Assert.True(session.State.Ime!.IsComposing);
        Assert.Equal("nihao", session.State.Ime.Input);
        Assert.Equal(string.Empty, session.State.Text);
    }

    [Fact]
    public void Composition_is_limited_to_the_configured_length()
    {
        var session = NewSession(maxCompositionLength: 8);
        Type(session, "abcdefghijk");

        Assert.Equal("abcdefgh", session.State.Ime!.Input);
    }

    [Fact]
    public void Composition_length_is_clamped_to_the_supported_range()
    {
        var session = new KeyboardSession(new KeyboardSessionOptions
        {
            Profile = InputProfiles.Text,
            ImeEngine = new FakeImeEngine(),
            ImeMaxCompositionLength = 1,
            ImeCandidatePageSize = 1
        });

        Type(session, "abcdefghijklmnop");

        Assert.Equal(8, session.State.Ime!.Input.Length);
        Assert.Single(session.State.Ime.Candidates);
    }

    [Fact]
    public void Ime_state_is_part_of_every_snapshot_and_is_null_without_an_engine()
    {
        var session = NewSession();
        var seen = new List<ImeCompositionState?>();
        session.StateChanged += (_, e) => seen.Add(e.State.Ime);

        Type(session, "ni");
        session.Execute(KeyCommand.ToggleIme);

        Assert.All(seen, state => Assert.NotNull(state));
        Assert.Null(NoEngineSession(InputProfiles.Text).State.Ime);
    }

    [Fact]
    public void Numeric_state_keeps_the_ime_state_but_has_no_ime_key()
    {
        var session = NewSession();
        session.Execute(KeyCommand.SwitchToNumericKeypad);

        Assert.NotNull(session.State.Ime);
        Assert.True(session.State.Layout.IsGrid);
        Assert.DoesNotContain(
            session.State.Layout.Grid!.Cells,
            cell => cell.Key.Command.Kind == KeyCommandKind.ToggleIme);
    }

    [Fact]
    public void Legacy_key_definition_constructor_has_no_accessible_name()
    {
        var oldKey = new KeyDefinition("key.a", KeyLabel.Literal("a"), KeyCommand.Insert("a"));
        Assert.Null(oldKey.AccessibleName);

        var named = new KeyDefinition(
            "key.named",
            KeyLabel.Literal("中"),
            KeyCommand.ToggleIme,
            1.0,
            KeyStyle.Action,
            false,
            null,
            null,
            1,
            1,
            null,
            KeyLabel.Literal("中文输入，切换到英文"));
        Assert.Equal("中文输入，切换到英文", named.AccessibleName!.Value.Text);
    }

    [Fact]
    public void Ime_layout_overload_rejects_a_mismatched_key()
    {
        var wrongCommand = new KeyDefinition("ime.toggle", KeyLabel.Literal("中"), KeyCommand.Clear, 1.5);
        Assert.Throws<ArgumentException>(
            () => KeyboardLayouts.Full(FullKeyboardLayoutKind.AnsiStyle, FullKeyboardPage.Main, wrongCommand));

        var wrongWidth = new KeyDefinition("ime.toggle", KeyLabel.Literal("中"), KeyCommand.ToggleIme, 2.0);
        Assert.Throws<ArgumentException>(
            () => KeyboardLayouts.Full(FullKeyboardLayoutKind.AnsiStyle, FullKeyboardPage.Main, wrongWidth));

        var noIme = new KeyDefinition("ime.toggle", KeyLabel.Literal("中"), KeyCommand.ToggleIme, 1.0);
        var phone = KeyboardLayouts.Full(FullKeyboardLayoutKind.PhoneHybrid, FullKeyboardPage.Main, noIme);
        Assert.Equal(1.0, phone.Rows[3].Keys.Single(key => key.Id == "ime.toggle").WidthUnits);
        Assert.Equal(10d, phone.Rows[3].TotalUnits);
    }

    private sealed class CustomProfile : InputProfile
    {
        public override string Id => "custom-text";

        public override bool TryValidate(string text, out string normalized, out InputValidationError? error)
        {
            normalized = text;
            error = null;
            return true;
        }
    }

    private sealed class NumericImeProfile : InputProfile
    {
        public override string Id => "custom-numeric-ime";

        public override KeyboardMode DefaultMode => KeyboardMode.NumericKeypad;

        public override bool AllowsFullKeyboard => false;

        public override bool AllowsIme => true;

        public override bool TryValidate(string text, out string normalized, out InputValidationError? error)
        {
            normalized = text;
            error = null;
            return true;
        }
    }
    [Fact]
    public void The_legacy_keyboard_state_constructor_has_no_ime_state()
    {
        var layout = KeyboardLayouts.Full(FullKeyboardLayoutKind.AnsiStyle);
        var state = new KeyboardState(
            "abc",
            3,
            KeyboardMode.FullKeyboard,
            isShiftActive: false,
            layout,
            FullKeyboardLayoutKind.AnsiStyle,
            FullKeyboardPage.Main,
            NumericKeypadLayoutKind.Numpad,
            error: null,
            isDirty: false,
            isCancelled: false);

        Assert.Null(state.Ime);
        Assert.Equal("abc", state.Text);
    }

    [Fact]
    public void The_symbol_page_has_no_ime_key_and_restores_it_on_return()
    {
        var session = NewSession(layout: FullKeyboardLayoutKind.PhoneHybrid);
        Type(session, "nihao");

        session.Execute(KeyCommand.SwitchToSymbolPage);

        Assert.Equal("你好", session.State.Text);
        Assert.Equal(FullKeyboardPage.Symbols, session.State.Page);
        Assert.DoesNotContain(
            session.State.Layout.Rows.SelectMany(row => row.Keys),
            key => key.Command.Kind == KeyCommandKind.ToggleIme);
        Assert.Equal(1.0, session.State.Layout.Rows[4].Keys.Single(key => key.Id == "key.space").WidthUnits);

        session.Execute(KeyCommand.SwitchToMainPage);
        Assert.Equal(FullKeyboardPage.Main, session.State.Page);
        Assert.Contains(
            session.State.Layout.Rows.SelectMany(row => row.Keys),
            key => key.Command.Kind == KeyCommandKind.ToggleIme);
        Assert.Equal(2.5, session.State.Layout.Rows[3].Keys.Single(key => key.Id == "key.space").WidthUnits);
    }

    [Fact]
    public void Candidate_page_size_can_change_and_is_clamped()
    {
        var session = NewSession(pageSize: 6);
        Type(session, "ni");

        Assert.Equal(6, session.State.Ime!.PageSize);
        Assert.Equal(2, session.State.Ime.PageCount);

        session.SetImeCandidatePageSize(3);
        Assert.Equal(3, session.State.Ime!.PageSize);
        Assert.Equal(3, session.State.Ime.PageCount);
        Assert.Equal(0, session.State.Ime.PageIndex);

        session.SetImeCandidatePageSize(99);
        Assert.Equal(9, session.State.Ime!.PageSize);
        Assert.Equal(1, session.State.Ime.PageCount);

        session.SetImeCandidatePageSize(0);
        Assert.Equal(1, session.State.Ime!.PageSize);
        Assert.Equal(7, session.State.Ime.PageCount);
    }

}
