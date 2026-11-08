using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using TouchKeyboard.Core;
using Xunit;
using AvaloniaPath = Avalonia.Controls.Shapes.Path;

namespace TouchKeyboard.Avalonia.Tests;

public sealed class TouchKeyboardHostTests
{
    private sealed class Fixture : IDisposable
    {
        public Fixture(Window window, TouchKeyboardHost host, TouchKeyboardController controller)
        {
            Window = window;
            Host = host;
            Controller = controller;
        }

        public Window Window { get; }
        public TouchKeyboardHost Host { get; }
        public TouchKeyboardController Controller { get; }

        public void Dispose()
        {
            Controller.Dispose();
            Window.Close();
        }
    }

    private static Fixture CreateFixture(
        Control field,
        Action<TouchKeyboardOptions>? configure = null,
        ThemeVariant? variant = null,
        double width = 1024,
        double height = 700,
        double keyHeight = 60)
    {
        var host = new TouchKeyboardHost { AutoAttach = false };
        host.Configure(options =>
        {
            options.KeyHeight = keyHeight;
            options.MaxPanelWidth = 1000;
            options.NumericPanelMaxWidth = 600;
            options.HandlePhysicalKeyboard = true;
            configure?.Invoke(options);
        });

        field.Margin = new Thickness(20);
        field.HorizontalAlignment = HorizontalAlignment.Stretch;
        field.VerticalAlignment = VerticalAlignment.Top;

        var grid = new Grid();
        grid.Children.Add(field);
        grid.Children.Add(host);

        var window = new Window
        {
            Width = width,
            Height = height,
            Content = grid
        };

        if (variant is not null)
        {
            window.RequestedThemeVariant = variant;
        }

        var controller = new TouchKeyboardController(window, host);
        window.Show();
        Dispatcher.UIThread.RunJobs();
        return new Fixture(window, host, controller);
    }

    private static void Click(Window window, Control control)
    {
        Press(window, control);
        Release(window, control);
    }

    private static void Press(Window window, Control control)
    {
        Dispatcher.UIThread.RunJobs();
        var point = control.TranslatePoint(
            new Point(control.Bounds.Width / 2, control.Bounds.Height / 2),
            window);

        Assert.NotNull(point);
        window.MouseDown(point!.Value, MouseButton.Left, RawInputModifiers.None);
        Dispatcher.UIThread.RunJobs();
    }

    private static void Release(Window window, Control control)
    {
        Dispatcher.UIThread.RunJobs();
        var point = control.TranslatePoint(
            new Point(control.Bounds.Width / 2, control.Bounds.Height / 2),
            window);

        Assert.NotNull(point);
        window.MouseUp(point!.Value, MouseButton.Left, RawInputModifiers.None);
        Dispatcher.UIThread.RunJobs();
    }

    private static void ClickKey(Window window, TouchKeyboardHost host, Func<KeyDefinition, bool> predicate)
    {
        Dispatcher.UIThread.RunJobs();
        var key = host.GetVisualDescendants()
            .OfType<TouchKey>()
            .FirstOrDefault(candidate => predicate(candidate.Definition));

        Assert.NotNull(key);
        Click(window, key!);
    }

    [AvaloniaFact]
    public void Clicking_a_text_box_opens_the_panel_and_confirm_writes_back()
    {
        var textBox = new TextBox();
        TouchKeyboardInput.SetProfile(textBox, "text");
        using var fixture = CreateFixture(textBox);

        Click(fixture.Window, textBox);
        Assert.True(fixture.Host.IsOpen);
        Assert.NotNull(fixture.Host.Session);
        Assert.Equal("text", fixture.Host.Session!.Profile.Id);

        ClickKey(fixture.Window, fixture.Host,
            key => key.Command == KeyCommand.Insert("a"));
        Assert.Equal("a", fixture.Host.Session!.State.Text);

        ClickKey(fixture.Window, fixture.Host,
            key => key.Command.Kind == KeyCommandKind.Submit);

        Assert.False(fixture.Host.IsOpen);
        Assert.Equal("a", textBox.Text);
    }

    [AvaloniaFact]
    public void Closing_the_panel_discards_edits()
    {
        var textBox = new TextBox { Text = "old" };
        TouchKeyboardInput.SetProfile(textBox, "text");
        using var fixture = CreateFixture(textBox);

        Click(fixture.Window, textBox);
        ClickKey(fixture.Window, fixture.Host,
            key => key.Command == KeyCommand.Insert("a"));
        Assert.Equal("olda", fixture.Host.Session!.State.Text);

        fixture.Host.Close();

        Assert.False(fixture.Host.IsOpen);
        Assert.Equal("old", textBox.Text);
    }

    [AvaloniaFact]
    public void Shift_produces_uppercase_from_the_full_keyboard()
    {
        var textBox = new TextBox();
        TouchKeyboardInput.SetProfile(textBox, "text");
        using var fixture = CreateFixture(textBox);

        Click(fixture.Window, textBox);
        ClickKey(fixture.Window, fixture.Host,
            key => key.Command.Kind == KeyCommandKind.ToggleShift);
        ClickKey(fixture.Window, fixture.Host,
            key => key.Command == KeyCommand.Insert("q"));

        Assert.Equal("Q", fixture.Host.Session!.State.Text);
    }

    [AvaloniaFact]
    public void Numeric_up_down_commits_the_edited_value()
    {
        var numeric = new NumericUpDown
        {
            Value = 12,
            Minimum = 0,
            Maximum = 9999
        };
        TouchKeyboardInput.SetProfile(numeric, "positive-integer");
        using var fixture = CreateFixture(numeric);

        Click(fixture.Window, numeric);
        Assert.True(fixture.Host.IsOpen);
        Assert.Equal("positive-integer", fixture.Host.Session!.Profile.Id);

        ClickKey(fixture.Window, fixture.Host,
            key => key.Command == KeyCommand.Insert("5"));
        Assert.Equal("125", fixture.Host.Session!.State.Text);

        ClickKey(fixture.Window, fixture.Host,
            key => key.Command.Kind == KeyCommandKind.Submit);

        Assert.False(fixture.Host.IsOpen);
        Assert.Equal(125m, numeric.Value);
    }

    [AvaloniaFact]
    public void Mode_switch_uses_the_general_numeric_surface_and_returns()
    {
        var textBox = new TextBox();
        TouchKeyboardInput.SetProfile(textBox, "text");
        using var fixture = CreateFixture(textBox);

        Click(fixture.Window, textBox);
        ClickKey(fixture.Window, fixture.Host,
            key => key.Command.Kind == KeyCommandKind.SwitchToNumericKeypad);
        Assert.Equal(KeyboardMode.NumericKeypad, fixture.Host.Session!.State.Mode);

        ClickKey(fixture.Window, fixture.Host,
            key => key.Command.Kind == KeyCommandKind.SwitchToFullKeyboard);
        Assert.Equal(KeyboardMode.FullKeyboard, fixture.Host.Session!.State.Mode);
    }

    [AvaloniaFact]
    public void Read_only_and_keyboard_disabled_fields_do_not_open()
    {
        var readOnly = new TextBox { IsReadOnly = true };
        TouchKeyboardInput.SetProfile(readOnly, "text");
        using (var fixture = CreateFixture(readOnly))
        {
            Click(fixture.Window, readOnly);
            Assert.False(fixture.Host.IsOpen);
        }

        var disabled = new TextBox();
        TouchKeyboardInput.SetProfile(disabled, "text");
        TouchKeyboardInput.SetEnabled(disabled, false);
        using (var fixture = CreateFixture(disabled))
        {
            Click(fixture.Window, disabled);
            Assert.False(fixture.Host.IsOpen);
        }
    }

    [AvaloniaFact]
    public void Physical_keyboard_input_goes_to_the_session_while_open()
    {
        var textBox = new TextBox();
        TouchKeyboardInput.SetProfile(textBox, "text");
        using var fixture = CreateFixture(textBox);

        Click(fixture.Window, textBox);
        fixture.Host.Focus();
        Dispatcher.UIThread.RunJobs();

        fixture.Window.KeyTextInput("b");
        Dispatcher.UIThread.RunJobs();

        Assert.Equal("b", fixture.Host.Session!.State.Text);
    }

    [AvaloniaFact]
    public void Date_picker_opens_with_a_date_profile_and_commits_selected_date()
    {
        var picker = new CalendarDatePicker();
        TouchKeyboardInput.SetProfile(picker, "date-slash");
        using var fixture = CreateFixture(picker);

        Click(fixture.Window, picker);
        Assert.True(fixture.Host.IsOpen);
        Assert.Equal("date-slash", fixture.Host.Session!.Profile.Id);

        fixture.Host.Session.Execute(KeyCommand.Insert("2026/11/06"));
        ClickKey(fixture.Window, fixture.Host,
            key => key.Command.Kind == KeyCommandKind.Submit);

        Assert.False(fixture.Host.IsOpen);
        Assert.Equal(new DateTime(2026, 11, 6), picker.SelectedDate);
    }

    [AvaloniaFact]
    public void Host_renders_a_frame_while_open()
    {
        var textBox = new TextBox { Text = "25.0" };
        TouchKeyboardInput.SetProfile(textBox, "text");
        using var fixture = CreateFixture(textBox);

        Click(fixture.Window, textBox);
        AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        var frame = fixture.Window.CaptureRenderedFrame();

        Assert.NotNull(frame);
    }

    [AvaloniaFact]
    public void Auto_attach_controller_opens_without_code_behind()
    {
        var textBox = new TextBox();
        TouchKeyboardInput.SetProfile(textBox, "text");
        textBox.Margin = new Thickness(20);
        textBox.HorizontalAlignment = HorizontalAlignment.Stretch;
        textBox.VerticalAlignment = VerticalAlignment.Top;

        var host = new TouchKeyboardHost();
        host.Configure(options =>
        {
            options.KeyHeight = 60;
            options.MaxPanelWidth = 1000;
        });

        var grid = new Grid();
        grid.Children.Add(textBox);
        grid.Children.Add(host);

        var window = new Window
        {
            Width = 1024,
            Height = 700,
            Content = grid
        };

        window.Show();
        Dispatcher.UIThread.RunJobs();
        Click(window, textBox);

        Assert.True(host.IsOpen);

        host.Close();
        window.Close();
    }

    [AvaloniaFact]
    public void Symbol_page_opens_from_phone_hybrid_and_returns_to_main()
    {
        var textBox = new TextBox();
        TouchKeyboardInput.SetProfile(textBox, "text");
        using var fixture = CreateFixture(
            textBox,
            options => options.FullKeyboardLayout = FullKeyboardLayoutKind.PhoneHybrid);

        Click(fixture.Window, textBox);
        Assert.Equal("full-phone-hybrid", fixture.Host.Session!.State.Layout.Id);

        ClickKey(fixture.Window, fixture.Host,
            key => key.Command.Kind == KeyCommandKind.SwitchToSymbolPage);
        Assert.Equal(FullKeyboardPage.Symbols, fixture.Host.Session!.State.Page);
        Assert.Equal("full-symbol-page", fixture.Host.Session!.State.Layout.Id);

        ClickKey(fixture.Window, fixture.Host,
            key => key.Command == KeyCommand.Insert("!"));
        Assert.Equal("!", fixture.Host.Session!.State.Text);

        ClickKey(fixture.Window, fixture.Host,
            key => key.Command.Kind == KeyCommandKind.SwitchToMainPage);
        Assert.Equal(FullKeyboardPage.Main, fixture.Host.Session!.State.Page);
        Assert.Equal("full-phone-hybrid", fixture.Host.Session!.State.Layout.Id);
    }

    [AvaloniaFact]
    public void Numpad_renders_grid_spans_for_zero_and_confirm()
    {
        var numeric = new NumericUpDown
        {
            Value = 12,
            Minimum = 0,
            Maximum = 9999
        };
        TouchKeyboardInput.SetProfile(numeric, "positive-integer");
        using var fixture = CreateFixture(numeric);

        Click(fixture.Window, numeric);

        var keys = fixture.Host.GetVisualDescendants().OfType<TouchKey>().ToList();
        var zero = keys.Single(key => key.Definition.Command == KeyCommand.Insert("0"));
        Assert.Equal(3, Grid.GetRow(zero));
        Assert.Equal(2, Grid.GetColumn(zero));
        Assert.Equal(2, Grid.GetColumnSpan(zero));

        var confirm = keys.Single(key => key.Definition.Command.Kind == KeyCommandKind.Submit);
        Assert.Equal(2, Grid.GetRow(confirm));
        Assert.Equal(4, Grid.GetColumn(confirm));
        Assert.Equal(2, Grid.GetRowSpan(confirm));

        // The empty left row-2 slot renders nothing at all.
        Assert.DoesNotContain(
            keys,
            key => Grid.GetRow(key) == 2 && Grid.GetColumn(key) == 0);
    }

    [AvaloniaFact]
    public void Icon_keys_render_vectors_and_expose_localized_accessible_names()
    {
        var textBox = new TextBox();
        TouchKeyboardInput.SetProfile(textBox, "text");
        using var fixture = CreateFixture(textBox, options => options.Localizer = TouchKeyboardLocalizer.ZhCn);

        Click(fixture.Window, textBox);

        var backspace = fixture.Host.GetVisualDescendants()
            .OfType<TouchKey>()
            .Single(key => key.Definition.Glyph == KeyGlyph.Backspace);
        var backspacePath = backspace.GetVisualDescendants().OfType<AvaloniaPath>().Single();
        Assert.True(backspacePath.IsVisible);
        Assert.NotNull(backspacePath.Data);
        Assert.Equal("退格", AutomationProperties.GetName(backspace));

        var peer = ControlAutomationPeer.CreatePeerForElement(backspace);
        Assert.NotNull(peer);
        Assert.True(peer!.IsControlElement());
        Assert.Equal(AutomationControlType.Button, peer.GetAutomationControlType());
        Assert.Equal("退格", peer.GetName());

        var letter = fixture.Host.GetVisualDescendants()
            .OfType<TouchKey>()
            .Single(key => key.Definition.Id == "key.char.0071");
        Assert.Equal("q", AutomationProperties.GetName(letter));
        Assert.DoesNotContain(
            letter.GetVisualDescendants().OfType<Viewbox>(),
            box => box.IsVisible);

        ClickKey(fixture.Window, fixture.Host,
            key => key.Command.Kind == KeyCommandKind.ToggleShift);

        var shift = fixture.Host.GetVisualDescendants()
            .OfType<TouchKey>()
            .First(key => key.Definition.Glyph == KeyGlyph.Shift);
        var shiftPath = shift.GetVisualDescendants().OfType<AvaloniaPath>().Single();
        Assert.NotNull(shiftPath.Fill);
        Assert.Null(shiftPath.Stroke);
    }

    [AvaloniaFact]
    public void Phone_hybrid_renders_z_directly_under_s()
    {
        var textBox = new TextBox();
        TouchKeyboardInput.SetProfile(textBox, "text");
        using var fixture = CreateFixture(
            textBox,
            options => options.FullKeyboardLayout = FullKeyboardLayoutKind.PhoneHybrid);

        Click(fixture.Window, textBox);

        var keys = fixture.Host.GetVisualDescendants().OfType<TouchKey>().ToList();
        var sKey = keys.Single(key => key.Definition.Id == "key.char.0073");
        var zKey = keys.Single(key => key.Definition.Id == "key.char.007a");

        var sOrigin = sKey.TranslatePoint(new Point(0, 0), fixture.Window);
        var zOrigin = zKey.TranslatePoint(new Point(0, 0), fixture.Window);

        Assert.NotNull(sOrigin);
        Assert.NotNull(zOrigin);
        Assert.Equal(sOrigin!.Value.X, zOrigin!.Value.X, 1);
    }

    [AvaloniaFact]
    public void The_editor_caret_follows_trailing_spaces()
    {
        var session = new KeyboardSession(new KeyboardSessionOptions
        {
            Text = "a",
            Profile = InputProfiles.Text
        });
        var editor = new TouchKeyboardEditor { EditorFontSize = 28 };
        editor.Attach(session);

        try
        {
            var afterLetter = editor.CaretOffset;

            session.Execute(KeyCommand.Insert(" "));
            var afterSpace = editor.CaretOffset;
            Assert.True(afterSpace > afterLetter + 4, "the caret should advance by a space");

            session.Execute(KeyCommand.Insert(" "));
            Assert.True(editor.CaretOffset > afterSpace + 4, "the caret should advance for each space");

            session.Execute(KeyCommand.Insert("b"));
            Assert.True(editor.CaretOffset > afterSpace + 4);
        }
        finally
        {
            editor.Detach();
        }
    }

    [AvaloniaFact]
    public void The_key_area_keeps_its_five_row_height_across_layouts()
    {
        var textBox = new TextBox();
        TouchKeyboardInput.SetProfile(textBox, "text");
        using var fixture = CreateFixture(textBox);

        Click(fixture.Window, textBox);
        var layoutView = fixture.Host.GetVisualDescendants().OfType<KeyboardLayoutView>().Single();
        var area = Assert.IsType<Border>(layoutView.Parent);
        var areaHeight = area.Bounds.Height;

        fixture.Host.Session!.Execute(KeyCommand.SwitchToNumericKeypad);
        Dispatcher.UIThread.RunJobs();
        AvaloniaHeadlessPlatform.ForceRenderTimerTick();

        Assert.Equal(areaHeight, area.Bounds.Height);
        Assert.True(layoutView.Bounds.Height < areaHeight);
        Assert.NotNull(fixture.Window.CaptureRenderedFrame());
    }

    [AvaloniaFact]
    public void Host_renders_the_symbol_page_and_the_numpad_frames()
    {
        var textBox = new TextBox();
        TouchKeyboardInput.SetProfile(textBox, "text");
        using var fixture = CreateFixture(
            textBox,
            options => options.FullKeyboardLayout = FullKeyboardLayoutKind.PhoneHybrid);

        Click(fixture.Window, textBox);
        ClickKey(fixture.Window, fixture.Host,
            key => key.Command.Kind == KeyCommandKind.SwitchToSymbolPage);
        AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        Assert.NotNull(fixture.Window.CaptureRenderedFrame());

        // The symbol page no longer carries a numeric switch; return to the main
        // page first, then open the Numpad.
        ClickKey(fixture.Window, fixture.Host,
            key => key.Command.Kind == KeyCommandKind.SwitchToMainPage);
        ClickKey(fixture.Window, fixture.Host,
            key => key.Command.Kind == KeyCommandKind.SwitchToNumericKeypad);
        AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        Assert.NotNull(fixture.Window.CaptureRenderedFrame());
    }

    [AvaloniaFact]
    public void Host_follows_the_dark_theme_variant()
    {
        var textBox = new TextBox();
        TouchKeyboardInput.SetProfile(textBox, "text");
        using var fixture = CreateFixture(textBox, variant: ThemeVariant.Dark);

        Click(fixture.Window, textBox);
        Dispatcher.UIThread.RunJobs();

        Assert.Same(TouchKeyboardTheme.Dark, fixture.Host.ResolvedTheme);

        var confirm = fixture.Host.GetVisualDescendants()
            .OfType<TouchKey>()
            .Single(key => key.Definition.Style == KeyStyle.Primary);
        Assert.Same(TouchKeyboardTheme.Dark.PrimaryKeyBackground, confirm.Background);

        var close = fixture.Host.GetVisualDescendants()
            .OfType<TouchKey>()
            .First(key => key.Definition.Glyph == KeyGlyph.Backspace);
        Assert.Same(TouchKeyboardTheme.Dark.ActionKeyBackground, close.Background);
    }

    [AvaloniaFact]
    public void Changing_the_window_variant_reapplies_the_theme()
    {
        var textBox = new TextBox();
        TouchKeyboardInput.SetProfile(textBox, "text");
        using var fixture = CreateFixture(textBox);

        Click(fixture.Window, textBox);
        Dispatcher.UIThread.RunJobs();
        Assert.Same(TouchKeyboardTheme.Light, fixture.Host.ResolvedTheme);

        fixture.Window.RequestedThemeVariant = ThemeVariant.Dark;
        Dispatcher.UIThread.RunJobs();

        Assert.Same(TouchKeyboardTheme.Dark, fixture.Host.ResolvedTheme);
        var confirm = fixture.Host.GetVisualDescendants()
            .OfType<TouchKey>()
            .Single(key => key.Definition.Style == KeyStyle.Primary);
        Assert.Same(TouchKeyboardTheme.Dark.PrimaryKeyBackground, confirm.Background);
    }

    [AvaloniaFact]
    public void An_explicit_theme_overrides_the_window_variant()
    {
        var textBox = new TextBox();
        TouchKeyboardInput.SetProfile(textBox, "text");
        using var fixture = CreateFixture(
            textBox,
            options => options.Theme = TouchKeyboardTheme.Light,
            ThemeVariant.Dark);

        Click(fixture.Window, textBox);
        Dispatcher.UIThread.RunJobs();

        Assert.Same(TouchKeyboardTheme.Light, fixture.Host.ResolvedTheme);
    }

    [AvaloniaFact]
    public void Pressing_a_key_scales_it_and_shows_the_pressed_overlay()
    {
        var textBox = new TextBox();
        TouchKeyboardInput.SetProfile(textBox, "text");
        using var fixture = CreateFixture(textBox);

        Click(fixture.Window, textBox);
        Dispatcher.UIThread.RunJobs();

        var key = fixture.Host.GetVisualDescendants()
            .OfType<TouchKey>()
            .First(candidate => candidate.Definition.Command == KeyCommand.Insert("q"));

        Press(fixture.Window, key);
        var pressed = Assert.IsType<ScaleTransform>(key.RenderTransform);
        Assert.True(pressed.ScaleX < 1);
        Assert.True(pressed.ScaleY < 1);
        Assert.Contains(
            key.GetVisualDescendants().OfType<Border>(),
            border => border.IsVisible && border.Background is not null);

        Release(fixture.Window, key);
        var released = Assert.IsType<ScaleTransform>(key.RenderTransform);
        Assert.Equal(1, released.ScaleX);
        Assert.Equal("q", fixture.Host.Session!.State.Text);
        Assert.DoesNotContain(
            key.GetVisualDescendants().OfType<Border>(),
            border => border.IsVisible && border.Background is not null);
    }

    [AvaloniaFact]
    public void Compact_icons_render_smaller_than_full_size_icons()
    {
        var textBox = new TextBox();
        TouchKeyboardInput.SetProfile(textBox, "text");
        using var fixture = CreateFixture(textBox);

        Click(fixture.Window, textBox);
        Dispatcher.UIThread.RunJobs();

        var keys = fixture.Host.GetVisualDescendants().OfType<TouchKey>().ToList();
        var backspaceBox = keys.Single(key => key.Definition.Glyph == KeyGlyph.Backspace)
            .GetVisualDescendants().OfType<Viewbox>().Single();
        var shiftBox = keys.First(key => key.Definition.Glyph == KeyGlyph.Shift)
            .GetVisualDescendants().OfType<Viewbox>().Single();

        Assert.True(shiftBox.Width < backspaceBox.Width);
    }

    [AvaloniaFact]
    public void Space_and_password_visibility_use_vector_icons()
    {
        var password = new TextBox { PasswordChar = '*' };
        TouchKeyboardInput.SetProfile(password, "password");
        using var fixture = CreateFixture(
            password,
            options => options.Localizer = TouchKeyboardLocalizer.EnUs);

        Click(fixture.Window, password);
        Dispatcher.UIThread.RunJobs();

        var space = fixture.Host.GetVisualDescendants()
            .OfType<TouchKey>()
            .Single(key => key.Definition.Glyph == KeyGlyph.Space);
        Assert.NotNull(space.GetVisualDescendants().OfType<AvaloniaPath>().Single(path => path.IsVisible).Data);

        var reveal = fixture.Host.GetVisualDescendants()
            .OfType<Control>()
            .FirstOrDefault(control => AutomationProperties.GetName(control) == "Show password");
        Assert.NotNull(reveal);
        var revealIcon = reveal!.GetVisualDescendants().OfType<AvaloniaPath>().Single(path => path.IsVisible);
        var showPasswordGeometry = revealIcon.Data;
        Assert.NotNull(showPasswordGeometry);

        Click(fixture.Window, reveal);
        Assert.Equal("Hide password", AutomationProperties.GetName(reveal));
        Assert.Same(revealIcon, reveal.GetVisualDescendants().OfType<AvaloniaPath>().Single(path => path.IsVisible));
        Assert.NotSame(showPasswordGeometry, revealIcon.Data);
    }
    private sealed class HostFakeImeEngine : IImeEngine
    {
        public string LanguageTag => "zh-Hans";

        public bool TryQuery(string input, int maxCandidates, out IReadOnlyList<ImeCandidate> candidates)
        {
            var values = input switch
            {
                "ni" => new[] { "你", "尼", "泥", "拟", "逆", "妮", "倪" },
                "nihao" => new[] { "你好", "拟好", "你" },
                "woaibeijing" => new[] { "我爱北京", "我爱背景" },
                "gege" => new[] { "各个", "各个方向", "各个行业", "各国", "各自", "各部分", "各地" },
                "verylong" => new[]
                {
                    "中华人民共和国", "中国人民解放军", "中华人民共和国国歌",
                    "全国人民代表大会常务委员会", "人民大会堂", "中华人民共和国中央人民政府",
                    "国务院办公厅", "最高人民法院", "最高人民检察院"
                },
                _ => new[] { "候选" + input }
            };

            candidates = values
                .Take(Math.Max(0, maxCandidates))
                .Select(text => new ImeCandidate(text))
                .ToArray();
            return candidates.Count > 0;
        }

        public bool TryMapPunctuation(char input, out string mapped)
        {
            if (input == ',')
            {
                mapped = "，";
                return true;
            }

            mapped = string.Empty;
            return false;
        }
    }

    private sealed class PlainEditorSurface : IEditorSurface
    {
        private readonly TextBlock _view = new TextBlock { Focusable = false };
        private KeyboardSession? _session;

        public Control View => _view;

        public void Attach(KeyboardSession session)
        {
            Detach();
            _session = session;
            _view.Text = session.State.Text;
            session.StateChanged += OnStateChanged;
        }

        public void Detach()
        {
            if (_session is not null)
            {
                _session.StateChanged -= OnStateChanged;
                _session = null;
            }
        }

        private void OnStateChanged(object? sender, KeyboardStateChangedEventArgs e)
            => _view.Text = e.State.Text;
    }

    private static void Type(Window window, KeyboardSession session, string text)
    {
        foreach (var letter in text)
        {
            session.Execute(KeyCommand.Insert(letter.ToString()));
        }

        Dispatcher.UIThread.RunJobs();
    }

    private static TouchKey? FindImeKey(TouchKeyboardHost host)
        => host.GetVisualDescendants()
            .OfType<TouchKey>()
            .FirstOrDefault(key => key.Definition.Command.Kind == KeyCommandKind.ToggleIme);

    private static Control? FindAccessible(TouchKeyboardHost host, string prefix)
        => host.GetVisualDescendants()
            .OfType<Control>()
            .FirstOrDefault(control =>
                AutomationProperties.GetName(control)?.StartsWith(prefix, StringComparison.Ordinal) == true);

    [AvaloniaFact]
    public void Text_with_an_engine_shows_the_ime_key_and_fixed_candidate_bar()
    {
        var textBox = new TextBox();
        TouchKeyboardInput.SetProfile(textBox, "text");
        using var fixture = CreateFixture(
            textBox,
            options =>
            {
                options.Localizer = TouchKeyboardLocalizer.ZhCn;
                options.UseIme(new HostFakeImeEngine());
            });

        Click(fixture.Window, textBox);
        Dispatcher.UIThread.RunJobs();

        Assert.NotNull(fixture.Host.Session!.State.Ime);
        Assert.True(fixture.Host.CandidateBar.IsVisible);
        Assert.Equal(44, fixture.Host.CandidateBar.Height);
        Assert.Equal(44, fixture.Host.CandidateBar.Bounds.Height);

        var imeKey = FindImeKey(fixture.Host);
        Assert.NotNull(imeKey);
        Assert.Equal("中文输入，切换到英文", AutomationProperties.GetName(imeKey!));
        Assert.Equal(1.5, imeKey!.Definition.WidthUnits);
    }

    [AvaloniaFact]
    public void No_engine_has_no_ime_key_or_candidate_bar_and_expands_space()
    {
        var textBox = new TextBox();
        TouchKeyboardInput.SetProfile(textBox, "text");
        using var fixture = CreateFixture(textBox);

        Click(fixture.Window, textBox);
        Dispatcher.UIThread.RunJobs();

        Assert.Null(fixture.Host.Session!.State.Ime);
        Assert.False(fixture.Host.CandidateBar.IsVisible);
        Assert.Null(FindImeKey(fixture.Host));

        var space = fixture.Host.GetVisualDescendants()
            .OfType<TouchKey>()
            .Single(key => key.Definition.Id == "key.space");
        Assert.Equal(7.0, space.Definition.WidthUnits);
    }

    [AvaloniaFact]
    public void Toggling_ime_keeps_the_key_and_does_not_change_the_space_width()
    {
        var textBox = new TextBox();
        TouchKeyboardInput.SetProfile(textBox, "text");
        using var fixture = CreateFixture(
            textBox,
            options =>
            {
                options.Localizer = TouchKeyboardLocalizer.ZhCn;
                options.UseIme(new HostFakeImeEngine());
            });

        Click(fixture.Window, textBox);
        Dispatcher.UIThread.RunJobs();

        var space = fixture.Host.GetVisualDescendants()
            .OfType<TouchKey>()
            .Single(key => key.Definition.Id == "key.space");
        var widthBefore = space.Bounds.Width;

        Click(fixture.Window, FindImeKey(fixture.Host)!);
        Dispatcher.UIThread.RunJobs();

        Assert.False(fixture.Host.Session!.State.Ime!.IsEnabled);
        var imeKey = FindImeKey(fixture.Host);
        Assert.NotNull(imeKey);
        Assert.Equal("英文输入，切换到中文", AutomationProperties.GetName(imeKey!));
        Assert.Equal(1.5, imeKey!.Definition.WidthUnits);

        space = fixture.Host.GetVisualDescendants()
            .OfType<TouchKey>()
            .Single(key => key.Definition.Id == "key.space");
        Assert.Equal(5.5, space.Definition.WidthUnits);
        Assert.Equal(widthBefore, space.Bounds.Width, 1);
        Assert.Equal(44, fixture.Host.CandidateBar.Bounds.Height);
    }

    [AvaloniaFact]
    public void Ime_incapable_profiles_have_no_ime_key_or_candidate_row()
    {
        var profiles = new[]
        {
            "password",
            "ascii-token",
            "integer",
            "positive-decimal",
            "date-slash",
            "ip-address"
        };

        foreach (var profile in profiles)
        {
            var textBox = new TextBox();
            TouchKeyboardInput.SetProfile(textBox, profile);
            using var fixture = CreateFixture(
                textBox,
                options => options.UseIme(new HostFakeImeEngine()));

            Click(fixture.Window, textBox);
            Dispatcher.UIThread.RunJobs();

            Assert.NotNull(fixture.Host.Session);
            Assert.Null(fixture.Host.Session!.State.Ime);
            Assert.False(fixture.Host.CandidateBar.IsVisible);
            Assert.Null(FindImeKey(fixture.Host));
        }
    }

    [AvaloniaFact]
    public void Candidate_bar_height_stays_fixed_while_composing_and_paging()
    {
        var textBox = new TextBox();
        TouchKeyboardInput.SetProfile(textBox, "text");
        using var fixture = CreateFixture(
            textBox,
            options =>
            {
                options.Localizer = TouchKeyboardLocalizer.ZhCn;
                options.Ime.CandidatePageSize = 3;
                options.UseIme(new HostFakeImeEngine());
            });

        Click(fixture.Window, textBox);
        Dispatcher.UIThread.RunJobs();
        var height = fixture.Host.CandidateBar.Bounds.Height;

        Type(fixture.Window, fixture.Host.Session!, "ni");
        Assert.True(fixture.Host.Session!.State.Ime!.IsComposing);
        Assert.Equal(height, fixture.Host.CandidateBar.Bounds.Height, 1);
        Assert.True(fixture.Host.Session.State.Ime.PageCount > 1);

        var next = FindAccessible(fixture.Host, "下一页候选");
        Assert.NotNull(next);
        Assert.True(next!.Bounds.Height >= 44, $"next height {next.Bounds.Height}");
        Assert.True(next.Bounds.Width >= 56, $"next width {next.Bounds.Width}");

        Click(fixture.Window, next);
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(1, fixture.Host.Session!.State.Ime!.PageIndex);
        Assert.Equal(height, fixture.Host.CandidateBar.Bounds.Height, 1);
    }

    [AvaloniaFact]
    public void Candidate_tap_commits_the_candidate()
    {
        var textBox = new TextBox();
        TouchKeyboardInput.SetProfile(textBox, "text");
        using var fixture = CreateFixture(
            textBox,
            options =>
            {
                options.Localizer = TouchKeyboardLocalizer.ZhCn;
                options.UseIme(new HostFakeImeEngine());
            });

        Click(fixture.Window, textBox);
        Type(fixture.Window, fixture.Host.Session!, "nihao");
        AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        Dispatcher.UIThread.RunJobs();

        var candidate = FindAccessible(fixture.Host, "第 1 个候选");
        Assert.NotNull(candidate);
        Assert.True(candidate!.Bounds.Height >= 44, $"candidate height {candidate.Bounds.Height}");
        Assert.True(candidate.Bounds.Width >= 44, $"candidate width {candidate.Bounds.Width}");
        var candidateList = fixture.Host.CandidateList;
        var candidateButtons = new List<Control>();
        for (var index = 1; index <= 9; index++)
        {
            var button = FindAccessible(fixture.Host, $"第 {index} 个候选");
            if (button is null)
            {
                break;
            }

            candidateButtons.Add(button);
        }

        Assert.NotEmpty(candidateButtons);
        Assert.DoesNotContain(
            fixture.Host.CandidateBar.GetVisualDescendants().OfType<ScrollViewer>(),
            scroll => scroll.IsVisible);
        var candidateWidth = candidateButtons.Sum(button => button.Bounds.Width)
                              + 6 * (candidateButtons.Count - 1);
        Assert.True(
            candidateWidth < candidateList.Bounds.Width - 20,
            $"2–3 candidates must keep their natural width: {candidateWidth} / {candidateList.Bounds.Width}");

        Press(fixture.Window, candidate);
        Dispatcher.UIThread.RunJobs();
        var overlay = candidate.GetVisualDescendants()
            .OfType<Border>()
            .FirstOrDefault(border => border.IsVisible && border.Background is not null);
        Assert.NotNull(overlay);
        Assert.Equal(candidate.Bounds.Width, overlay!.Bounds.Width, 1);

        Release(fixture.Window, candidate);
        Dispatcher.UIThread.RunJobs();

        Assert.Equal("你好", fixture.Host.Session!.State.Text);
        Assert.False(fixture.Host.Session.State.Ime!.IsComposing);
    }

    [AvaloniaFact]
    public void Physical_letters_enter_the_composition_and_enter_commits()
    {
        var textBox = new TextBox();
        TouchKeyboardInput.SetProfile(textBox, "text");
        using var fixture = CreateFixture(
            textBox,
            options => options.UseIme(new HostFakeImeEngine()));

        Click(fixture.Window, textBox);
        fixture.Host.Focus();
        Dispatcher.UIThread.RunJobs();

        var session = fixture.Host.Session!;
        fixture.Window.KeyTextInput("n");
        fixture.Window.KeyTextInput("i");
        Dispatcher.UIThread.RunJobs();
        Assert.Equal("ni", session.State.Ime!.Input);

        fixture.Window.KeyPress(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, null);
        Dispatcher.UIThread.RunJobs();

        Assert.Equal("你", session.State.Text);
        Assert.False(session.State.Ime!.IsComposing);
        Assert.False(fixture.Host.IsOpen);
    }

    [AvaloniaFact]
    public void Custom_editor_surface_still_shows_candidates_without_a_prefix()
    {
        var textBox = new TextBox();
        TouchKeyboardInput.SetProfile(textBox, "text");
        using var fixture = CreateFixture(
            textBox,
            options =>
            {
                options.UseIme(new HostFakeImeEngine());
                options.EditorSurfaceFactory = () => new PlainEditorSurface();
            });

        Click(fixture.Window, textBox);
        Type(fixture.Window, fixture.Host.Session!, "nihao");
        Dispatcher.UIThread.RunJobs();

        Assert.True(fixture.Host.CandidateBar.IsVisible);
        Assert.NotNull(FindAccessible(fixture.Host, "第 1 个候选"));
        Assert.DoesNotContain(
            fixture.Host.CandidateBar.GetVisualDescendants().OfType<TextBlock>(),
            text => text.Text == "nihao" && text.IsVisible);
    }

    [AvaloniaFact]
    public void Dark_theme_renders_the_candidate_bar_and_composition()
    {
        var textBox = new TextBox();
        TouchKeyboardInput.SetProfile(textBox, "text");
        using var fixture = CreateFixture(
            textBox,
            options => options.UseIme(new HostFakeImeEngine()),
            ThemeVariant.Dark);

        Click(fixture.Window, textBox);
        Type(fixture.Window, fixture.Host.Session!, "nihao");
        AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        Dispatcher.UIThread.RunJobs();

        Assert.Same(TouchKeyboardTheme.Dark, fixture.Host.ResolvedTheme);
        Assert.True(fixture.Host.CandidateBar.IsVisible);
        Assert.NotNull(fixture.Window.CaptureRenderedFrame());
    }

    [AvaloniaFact]
    public void Ime_panel_fits_a_1024_by_600_window()
    {
        var textBox = new TextBox();
        TouchKeyboardInput.SetProfile(textBox, "text");
        using var fixture = CreateFixture(
            textBox,
            options => options.UseIme(new HostFakeImeEngine()),
            width: 1024,
            height: 600,
            keyHeight: 0);

        Click(fixture.Window, textBox);
        Type(fixture.Window, fixture.Host.Session!, "ni");
        AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        Dispatcher.UIThread.RunJobs();

        Assert.True(fixture.Host.CandidateBar.IsVisible);
        Assert.Equal(44, fixture.Host.CandidateBar.Bounds.Height);
        Assert.True(fixture.Host.Panel.Bounds.Height <= 600, $"panel height {fixture.Host.Panel.Bounds.Height}");

        var bottom = fixture.Host.Panel.TranslatePoint(
            new Point(0, fixture.Host.Panel.Bounds.Height),
            fixture.Window);
        Assert.NotNull(bottom);
        Assert.True(bottom!.Value.Y <= 600.5, $"panel bottom {bottom.Value.Y}");
        Assert.NotNull(fixture.Window.CaptureRenderedFrame());
    }

    [AvaloniaFact]
    public void Default_enabled_false_starts_in_english_and_remember_mode_is_host_scoped()
    {
        var first = new TextBox();
        var second = new TextBox();
        TouchKeyboardInput.SetProfile(first, "text");
        TouchKeyboardInput.SetProfile(second, "text");
        var fields = new StackPanel();
        fields.Children.Add(first);
        fields.Children.Add(second);

        using var fixture = CreateFixture(
            fields,
            options =>
            {
                options.Localizer = TouchKeyboardLocalizer.ZhCn;
                options.Ime.DefaultEnabled = false;
                options.Ime.RememberMode = true;
                options.UseIme(new HostFakeImeEngine());
            });

        Click(fixture.Window, first);
        Dispatcher.UIThread.RunJobs();
        Assert.False(fixture.Host.Session!.State.Ime!.IsEnabled);

        Click(fixture.Window, FindImeKey(fixture.Host)!);
        Dispatcher.UIThread.RunJobs();
        Assert.True(fixture.Host.Session!.State.Ime!.IsEnabled);

        fixture.Host.Close();
        Click(fixture.Window, second);
        Dispatcher.UIThread.RunJobs();

        Assert.True(fixture.Host.Session!.State.Ime!.IsEnabled);
    }

    [Fact]
    public void Ime_options_have_expected_defaults_and_require_an_engine()
    {
        var options = new TouchKeyboardOptions();
        Assert.Null(options.Ime.Engine);
        Assert.Null(options.Ime.DefaultEnabled);
        Assert.True(options.Ime.RememberMode);
        Assert.Equal(9, options.Ime.CandidatePageSize);
        Assert.Equal(32, options.Ime.MaxCompositionLength);
        Assert.True(options.Ime.FullWidthPunctuation);
        Assert.NotNull(options.Style);
        Assert.Equal(1.0, options.Style.Scale);
        Assert.Equal(1.0, options.Style.FontScale);
        Assert.Equal(1.0, options.Style.IconScale);
        Assert.True(options.EnablePanelDrag);
        Assert.Throws<ArgumentNullException>(() => options.UseIme(null!));
    }

    [AvaloniaFact]
    public void English_localizer_names_the_ime_key_and_candidate()
    {
        var textBox = new TextBox();
        TouchKeyboardInput.SetProfile(textBox, "text");
        using var fixture = CreateFixture(
            textBox,
            options =>
            {
                options.Localizer = TouchKeyboardLocalizer.EnUs;
                options.UseIme(new HostFakeImeEngine());
            });

        Click(fixture.Window, textBox);
        Dispatcher.UIThread.RunJobs();

        var imeKey = FindImeKey(fixture.Host);
        Assert.NotNull(imeKey);
        Assert.Equal("Chinese input, switch to English", AutomationProperties.GetName(imeKey!));

        Type(fixture.Window, fixture.Host.Session!, "nihao");
        AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        Dispatcher.UIThread.RunJobs();
        var candidate = FindAccessible(fixture.Host, "Candidate 1");
        Assert.NotNull(candidate);
    }

    [AvaloniaFact]
    public void Escape_discards_a_pending_composition_without_writing_back()
    {
        var textBox = new TextBox();
        TouchKeyboardInput.SetProfile(textBox, "text");
        using var fixture = CreateFixture(
            textBox,
            options => options.UseIme(new HostFakeImeEngine()));

        Click(fixture.Window, textBox);
        var session = fixture.Host.Session!;
        Type(fixture.Window, session, "nihao");
        Assert.True(session.State.Ime!.IsComposing);

        fixture.Host.Focus();
        Dispatcher.UIThread.RunJobs();
        fixture.Window.KeyPress(Key.Escape, RawInputModifiers.None, PhysicalKey.Escape, null);
        Dispatcher.UIThread.RunJobs();

        Assert.False(fixture.Host.IsOpen);
        Assert.True(session.IsCancelled);
        Assert.True(string.IsNullOrEmpty(textBox.Text));
    }

    [AvaloniaFact]
    public void The_editor_caret_advances_with_the_composition()
    {
        var session = new KeyboardSession(new KeyboardSessionOptions
        {
            Profile = InputProfiles.Text,
            ImeEngine = new HostFakeImeEngine()
        });
        var editor = new TouchKeyboardEditor { EditorFontSize = 28 };
        editor.Attach(session);

        try
        {
            var before = editor.CaretOffset;
            session.Execute(KeyCommand.Insert("n"));
            var afterLetter = editor.CaretOffset;
            Assert.True(afterLetter > before, "the composition should advance the caret");

            session.Execute(KeyCommand.Insert("i"));
            Assert.True(editor.CaretOffset > afterLetter, "each composition letter should advance the caret");
        }
        finally
        {
            editor.Detach();
        }
    }

    [AvaloniaFact]
    public void Hiding_the_candidate_row_does_not_add_extra_spacing()
    {
        var plainBox = new TextBox();
        TouchKeyboardInput.SetProfile(plainBox, "text");
        using var plain = CreateFixture(plainBox);
        Click(plain.Window, plainBox);
        Dispatcher.UIThread.RunJobs();
        var plainHeight = plain.Host.Panel.Bounds.Height;

        var imeBox = new TextBox();
        TouchKeyboardInput.SetProfile(imeBox, "text");
        using var ime = CreateFixture(
            imeBox,
            options => options.UseIme(new HostFakeImeEngine()));
        Click(ime.Window, imeBox);
        Dispatcher.UIThread.RunJobs();
        var imeHeight = ime.Host.Panel.Bounds.Height;

        // Candidate bar 44 + its 10 px gap; the hidden row must not contribute.
        Assert.Equal(plainHeight + 54, imeHeight, 1);
    }

    [AvaloniaFact]
    public void Numeric_mode_keeps_the_candidate_row_visible_but_empty()
    {
        var textBox = new TextBox();
        TouchKeyboardInput.SetProfile(textBox, "text");
        using var fixture = CreateFixture(
            textBox,
            options => options.UseIme(new HostFakeImeEngine()));

        Click(fixture.Window, textBox);
        Dispatcher.UIThread.RunJobs();
        var height = fixture.Host.CandidateBar.Bounds.Height;
        var panelHeight = fixture.Host.Panel.Bounds.Height;

        fixture.Host.Session!.Execute(KeyCommand.SwitchToNumericKeypad);
        Dispatcher.UIThread.RunJobs();

        Assert.NotNull(fixture.Host.Session.State.Ime);
        Assert.True(fixture.Host.CandidateBar.IsVisible);
        Assert.Equal(height, fixture.Host.CandidateBar.Bounds.Height, 1);
        Assert.Equal(panelHeight, fixture.Host.Panel.Bounds.Height, 1);
        Assert.DoesNotContain(
            fixture.Host.CandidateBar.GetVisualDescendants().OfType<Control>(),
            control => control.IsVisible
                && AutomationProperties.GetName(control)?.StartsWith("第 ", StringComparison.Ordinal) == true);
    }

    [AvaloniaFact]
    public void Phone_hybrid_space_width_follows_the_ime_key_state()
    {
        foreach (var useIme in new[] { false, true })
        {
            var textBox = new TextBox();
            TouchKeyboardInput.SetProfile(textBox, "text");
            using var fixture = CreateFixture(
                textBox,
                options =>
                {
                    options.FullKeyboardLayout = FullKeyboardLayoutKind.PhoneHybrid;
                    if (useIme)
                    {
                        options.UseIme(new HostFakeImeEngine());
                    }
                });

            Click(fixture.Window, textBox);
            Dispatcher.UIThread.RunJobs();

            var space = fixture.Host.GetVisualDescendants()
                .OfType<TouchKey>()
                .Single(key => key.Definition.Id == "key.space");
            Assert.Equal(useIme ? 2.5 : 3.5, space.Definition.WidthUnits);
            Assert.Equal(10d, fixture.Host.Session!.State.Layout.Rows[3].TotalUnits);
            Assert.Equal(useIme, FindImeKey(fixture.Host) is not null);
        }
    }

    [AvaloniaFact]
    public void Phone_hybrid_candidate_row_is_the_first_row_and_heights_match_the_symbol_page()
    {
        var textBox = new TextBox();
        TouchKeyboardInput.SetProfile(textBox, "text");
        using var fixture = CreateFixture(
            textBox,
            options =>
            {
                options.FullKeyboardLayout = FullKeyboardLayoutKind.PhoneHybrid;
                options.UseIme(new HostFakeImeEngine());
            });

        Click(fixture.Window, textBox);
        Dispatcher.UIThread.RunJobs();

        var mainHeight = fixture.Host.Panel.Bounds.Height;
        Assert.True(fixture.Host.CandidateBar.IsVisible);
        Assert.Equal(60, fixture.Host.CandidateBar.Bounds.Height, 1);
        Assert.Equal(4, fixture.Host.Session!.State.Layout.Rows.Count);

        fixture.Host.Session.Execute(KeyCommand.SwitchToSymbolPage);
        Dispatcher.UIThread.RunJobs();

        Assert.False(fixture.Host.CandidateBar.IsVisible);
        Assert.Equal(5, fixture.Host.Session!.State.Layout.Rows.Count);
        Assert.Equal(mainHeight, fixture.Host.Panel.Bounds.Height, 1);

        fixture.Host.Session.Execute(KeyCommand.SwitchToMainPage);
        Dispatcher.UIThread.RunJobs();
        Assert.True(fixture.Host.CandidateBar.IsVisible);
        Assert.Equal(60, fixture.Host.CandidateBar.Bounds.Height, 1);
        Assert.Equal(mainHeight, fixture.Host.Panel.Bounds.Height, 1);

        fixture.Host.Session.Execute(KeyCommand.SwitchToNumericKeypad);
        Dispatcher.UIThread.RunJobs();
        Assert.True(fixture.Host.CandidateBar.IsVisible);
        Assert.Equal(60, fixture.Host.CandidateBar.Bounds.Height, 1);
        Assert.Equal(mainHeight, fixture.Host.Panel.Bounds.Height, 1);
    }

    [AvaloniaFact]
    public void Numeric_only_profiles_use_a_compact_four_row_keyboard_area()
    {
        var numeric = new NumericUpDown { Value = 12, Minimum = 0, Maximum = 9999 };
        TouchKeyboardInput.SetProfile(numeric, "positive-integer");
        using var fixture = CreateFixture(
            numeric,
            options => options.UseIme(new HostFakeImeEngine()));

        Click(fixture.Window, numeric);
        Dispatcher.UIThread.RunJobs();

        Assert.Null(fixture.Host.Session!.State.Ime);
        Assert.False(fixture.Host.CandidateBar.IsVisible);
        var layoutView = fixture.Host.GetVisualDescendants().OfType<KeyboardLayoutView>().Single();
        var area = Assert.IsType<Border>(layoutView.Parent);
        Assert.Equal(60 * 4 + 6 * 3, area.Bounds.Height, 1);
    }

    [AvaloniaFact]
    public void Style_scale_and_semantic_colors_apply_on_top_of_the_theme()
    {
        var textBox = new TextBox();
        TouchKeyboardInput.SetProfile(textBox, "text");
        var accent = Color.Parse("#FF0F766E");
        using var fixture = CreateFixture(
            textBox,
            options =>
            {
                options.Style.Scale = 0.8;
                options.Style.FontScale = 0.9;
                options.Style.IconScale = 1.5;
                options.Style.AccentColor = accent;
                options.Style.ConfirmTextColor = Colors.White;
                options.Style.SelectedCandidateTextColor = Colors.White;
                options.UseIme(new HostFakeImeEngine());
            });

        Click(fixture.Window, textBox);
        Dispatcher.UIThread.RunJobs();

        var layoutView = fixture.Host.GetVisualDescendants().OfType<KeyboardLayoutView>().Single();
        Assert.Equal(48, layoutView.KeyHeight, 1);
        Assert.Equal(800, fixture.Host.Panel.MaxWidth, 1);
        Assert.Equal(26 * 0.8 * 0.9, fixture.Host.ResolvedTheme.KeyFontSize, 1);
        Assert.Equal(1.5, fixture.Host.ResolvedTheme.IconScale, 3);

        var confirm = fixture.Host.GetVisualDescendants()
            .OfType<TouchKey>()
            .Single(key => key.Definition.Style == KeyStyle.Primary);
        var confirmBrush = Assert.IsType<SolidColorBrush>(confirm.Background);
        Assert.Equal(accent, confirmBrush.Color);

        Type(fixture.Window, fixture.Host.Session!, "nihao");
        AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        Dispatcher.UIThread.RunJobs();
        var candidate = FindAccessible(fixture.Host, "第 1 个候选");
        Assert.NotNull(candidate);
        var candidateBorder = Assert.IsAssignableFrom<Border>(candidate);
        var candidateBrush = Assert.IsType<SolidColorBrush>(candidateBorder.Background);
        Assert.Equal(accent, candidateBrush.Color);
        Assert.Equal(Colors.White, Assert.IsType<SolidColorBrush>(candidate.GetVisualDescendants()
            .OfType<TextBlock>()
            .Last(text => text.Text == "你好")
            .Foreground).Color);
    }

    [AvaloniaFact]
    public void Long_press_title_drag_moves_the_panel_and_stays_inside_the_window()
    {
        var textBox = new TextBox();
        TouchKeyboardInput.SetProfile(textBox, "text");
        using var fixture = CreateFixture(
            textBox,
            options =>
            {
                options.Localizer = TouchKeyboardLocalizer.EnUs;
                options.Style.Scale = 0.75;
                options.MaxPanelWidth = 900;
            });

        Click(fixture.Window, textBox);
        Dispatcher.UIThread.RunJobs();
        var title = fixture.Host.TitleRow;
        Assert.NotNull(title);

        Press(fixture.Window, title!);
        Assert.True(fixture.Host.PanelDragPending || fixture.Host.PanelDragging);
        fixture.Host.BeginPanelDragForTest();
        Dispatcher.UIThread.RunJobs();

        var center = title!.TranslatePoint(
            new Point(title.Bounds.Width / 2, title.Bounds.Height / 2),
            fixture.Window);
        Assert.NotNull(center);
        // Move far enough to hit the window clamp on at least one axis.
        var target = new Point(center!.Value.X + 400, center.Value.Y + 300);
        fixture.Window.MouseMove(target, RawInputModifiers.LeftMouseButton);
        Dispatcher.UIThread.RunJobs();

        Assert.True(fixture.Host.PanelOffsetX > 0, $"x={fixture.Host.PanelOffsetX}");
        Assert.True(fixture.Host.PanelOffsetY > 0, $"y={fixture.Host.PanelOffsetY}");

        var topLeft = fixture.Host.Panel.TranslatePoint(new Point(0, 0), fixture.Window);
        var bottomRight = fixture.Host.Panel.TranslatePoint(
            new Point(fixture.Host.Panel.Bounds.Width, fixture.Host.Panel.Bounds.Height),
            fixture.Window);
        Assert.NotNull(topLeft);
        Assert.NotNull(bottomRight);
        Assert.True(topLeft!.Value.X >= -0.5);
        Assert.True(topLeft.Value.Y >= -0.5);
        Assert.True(bottomRight!.Value.X <= fixture.Window.Bounds.Width + 0.5);
        Assert.True(bottomRight.Value.Y <= fixture.Window.Bounds.Height + 0.5);

        fixture.Window.MouseUp(target, MouseButton.Left, RawInputModifiers.None);
        fixture.Host.ResetPanelPosition();
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(0, fixture.Host.PanelOffsetX);
        Assert.Equal(0, fixture.Host.PanelOffsetY);

        var close = FindAccessible(fixture.Host, "Close");
        Assert.NotNull(close);
        Click(fixture.Window, close!);
        Assert.False(fixture.Host.IsOpen);
    }

    [AvaloniaFact]
    public void Long_candidate_text_is_shown_without_ellipsis()
    {
        var textBox = new TextBox();
        TouchKeyboardInput.SetProfile(textBox, "text");
        using var fixture = CreateFixture(
            textBox,
            options =>
            {
                options.Localizer = TouchKeyboardLocalizer.ZhCn;
                options.UseIme(new HostFakeImeEngine());
            });

        Click(fixture.Window, textBox);
        Type(fixture.Window, fixture.Host.Session!, "gege");
        AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        Dispatcher.UIThread.RunJobs();

        var expected = new[] { "各个", "各个方向", "各个行业", "各国", "各自", "各部分" };
        for (var index = 0; index < expected.Length; index++)
        {
            var candidate = FindAccessible(fixture.Host, $"第 {index + 1} 个候选");
            Assert.NotNull(candidate);
            var text = candidate!.GetVisualDescendants()
                .OfType<TextBlock>()
                .Last(block => block.Text == expected[index]);
            Assert.Equal(TextTrimming.None, text.TextTrimming);
            Assert.True(
                text.Bounds.Width + 1 >= text.DesiredSize.Width,
                $"{expected[index]} is clipped: {text.Bounds.Width} < {text.DesiredSize.Width}");
        }
    }

    [AvaloniaFact]
    public void A_single_candidate_keeps_its_natural_width()
    {
        var textBox = new TextBox();
        TouchKeyboardInput.SetProfile(textBox, "text");
        using var fixture = CreateFixture(
            textBox,
            options => options.UseIme(new HostFakeImeEngine()));

        Click(fixture.Window, textBox);
        Type(fixture.Window, fixture.Host.Session!, "x");
        AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        Dispatcher.UIThread.RunJobs();

        var candidate = FindAccessible(fixture.Host, "第 1 个候选");
        Assert.NotNull(candidate);
        var listWidth = fixture.Host.CandidateList.Bounds.Width;
        Assert.True(
            candidate!.Bounds.Width < listWidth - 20,
            $"a single candidate must not fill the row: {candidate.Bounds.Width} / {listWidth}");
    }

    [AvaloniaFact]
    public void Candidate_page_shrinks_to_fit_without_a_scrollbar()
    {
        var textBox = new TextBox();
        TouchKeyboardInput.SetProfile(textBox, "text");
        using var fixture = CreateFixture(
            textBox,
            options =>
            {
                options.Localizer = TouchKeyboardLocalizer.ZhCn;
                options.UseIme(new HostFakeImeEngine());
            });

        Click(fixture.Window, textBox);
        Type(fixture.Window, fixture.Host.Session!, "verylong");
        for (var i = 0; i < 3; i++)
        {
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
            Dispatcher.UIThread.RunJobs();
        }

        var ime = fixture.Host.Session!.State.Ime!;
        Assert.InRange(ime.PageSize, 1, 9);
        Assert.True(ime.PageSize < 9, $"expected an adaptive page size, got {ime.PageSize}");
        Assert.True(ime.PageCount > 1);
        Assert.DoesNotContain(
            fixture.Host.CandidateBar.GetVisualDescendants().OfType<ScrollViewer>(),
            scroll => scroll.IsVisible);

        var list = fixture.Host.CandidateList;
        var visible = fixture.Host.CandidateBar.GetVisualDescendants()
            .OfType<Control>()
            .Where(control => control.IsVisible
                              && AutomationProperties.GetName(control)?.StartsWith("第 ", StringComparison.Ordinal) == true)
            .ToList();
        Assert.Equal(ime.Candidates.Count, visible.Count);
        var visibleWidth = visible.Sum(control => control.Bounds.Width) + 6 * (visible.Count - 1);
        Assert.True(
            visibleWidth <= list.Bounds.Width + 1,
            $"candidate keys must fit: {visibleWidth} / {list.Bounds.Width}, pageSize={ime.PageSize}, count={visible.Count}");
        Assert.True(
            FindAccessible(fixture.Host, "下一页候选")!.Bounds.Width >= 56,
            "the 切页 key should be wider");
    }

}
