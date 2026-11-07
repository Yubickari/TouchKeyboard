using System;
using System.Linq;
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
        ThemeVariant? variant = null)
    {
        var host = new TouchKeyboardHost { AutoAttach = false };
        host.Configure(options =>
        {
            options.KeyHeight = 60;
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
            Width = 1024,
            Height = 700,
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
}
