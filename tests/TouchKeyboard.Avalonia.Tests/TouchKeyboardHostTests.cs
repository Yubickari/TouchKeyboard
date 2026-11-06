using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Threading;
using Avalonia.VisualTree;
using TouchKeyboard.Core;
using Xunit;

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

    private static Fixture CreateFixture(Control field)
    {
        var host = new TouchKeyboardHost { AutoAttach = false };
        host.Configure(options =>
        {
            options.KeyHeight = 60;
            options.MaxPanelWidth = 1000;
            options.NumericPanelMaxWidth = 600;
            options.HandlePhysicalKeyboard = true;
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

        var controller = new TouchKeyboardController(window, host);
        window.Show();
        Dispatcher.UIThread.RunJobs();
        return new Fixture(window, host, controller);
    }

    private static void Click(Window window, Control control)
    {
        Dispatcher.UIThread.RunJobs();
        var point = control.TranslatePoint(
            new Point(control.Bounds.Width / 2, control.Bounds.Height / 2),
            window);

        Assert.NotNull(point);
        window.MouseDown(point!.Value, MouseButton.Left, RawInputModifiers.None);
        window.MouseUp(point.Value, MouseButton.Left, RawInputModifiers.None);
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
}
