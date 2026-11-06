using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using TouchKeyboard.Core;

namespace TouchKeyboard.Avalonia;

/// <summary>
/// Watches a window's pointer presses, resolves an editable target and opens the
/// host. It restores focus when the panel closes and cancels on window deactivation.
/// </summary>
public sealed class TouchKeyboardController : IDisposable
{
    private readonly TopLevel _window;
    private readonly Window? _activityWindow;
    private readonly TouchKeyboardHost _host;
    private bool _disposed;

    /// <summary>Attaches the controller to a window and a host.</summary>
    public TouchKeyboardController(TopLevel window, TouchKeyboardHost host)
    {
        _window = window ?? throw new ArgumentNullException(nameof(window));
        _host = host ?? throw new ArgumentNullException(nameof(host));

        _window.AddHandler(
            InputElement.PointerPressedEvent,
            OnPointerPressed,
            RoutingStrategies.Tunnel,
            handledEventsToo: true);
        _activityWindow = window as Window;
        if (_activityWindow is not null)
        {
            _activityWindow.Deactivated += OnWindowDeactivated;
        }

        _host.Closed += OnHostClosed;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _window.RemoveHandler(InputElement.PointerPressedEvent, OnPointerPressed);
        if (_activityWindow is not null)
        {
            _activityWindow.Deactivated -= OnWindowDeactivated;
        }

        _host.Closed -= OnHostClosed;
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (_disposed || _host.IsOpen || !_host.Options.Enabled || e.Source is not Visual source)
        {
            return;
        }

        if (source.FindAncestorOfType<TouchKeyboardHost>() is not null)
        {
            return;
        }

        var target = ResolveTarget(source);
        if (target is null
            || !target.IsVisible
            || !target.IsEnabled
            || !TouchKeyboardInput.GetEnabled(target)
            || HasButtonAncestorBefore(source, target)
            || IsReadOnly(target))
        {
            return;
        }

        var profile = ResolveProfile(target);
        var adapter = CreateAdapter(target, profile);
        if (adapter is null)
        {
            return;
        }

        var session = new KeyboardSession(new KeyboardSessionOptions
        {
            Text = adapter.GetText(),
            Profile = profile,
            Label = TouchKeyboardInput.GetLabel(target),
            Unit = TouchKeyboardInput.GetUnit(target),
            MaxLength = adapter.MaxLength,
            AllowsNegativeSign = adapter.AllowsNegativeSign,
            Minimum = adapter.Minimum,
            Maximum = adapter.Maximum,
            FullKeyboardLayout = _host.Options.FullKeyboardLayout,
            NumericKeypadLayout = _host.Options.NumericKeypadLayout
        });

        _host.Open(session, adapter);
        e.Handled = true;
    }

    private void OnWindowDeactivated(object? sender, EventArgs e)
    {
        if (_host.IsOpen)
        {
            _host.Close();
        }
    }

    private void OnHostClosed(object? sender, TouchKeyboardClosedEventArgs e)
    {
        if (e.Target is { IsVisible: true, IsEnabled: true } target)
        {
            Dispatcher.UIThread.Post(() => target.Focus(), DispatcherPriority.Input);
        }
    }

    private static Control? ResolveTarget(Visual source)
    {
        Control? profileTarget = null;
        TextBox? textBox = null;
        NumericUpDown? numeric = null;
        CalendarDatePicker? datePicker = null;

        for (Visual? current = source; current is not null; current = current.GetVisualParent())
        {
            if (current is Control control)
            {
                if (profileTarget is null
                    && !string.IsNullOrWhiteSpace(TouchKeyboardInput.GetProfile(control)))
                {
                    profileTarget = control;
                }

                switch (control)
                {
                    case CalendarDatePicker picker:
                        datePicker ??= picker;
                        break;
                    case NumericUpDown upDown:
                        numeric ??= upDown;
                        break;
                    case TextBox box:
                        textBox ??= box;
                        break;
                }
            }

            if (current is TopLevel)
            {
                break;
            }
        }

        return profileTarget ?? (Control?)datePicker ?? (Control?)numeric ?? (Control?)textBox;
    }

    private static bool HasButtonAncestorBefore(Visual source, Control target)
    {
        for (Visual? current = source; current is not null; current = current.GetVisualParent())
        {
            if (ReferenceEquals(current, target))
            {
                return false;
            }

            if (current is Button)
            {
                return true;
            }

            if (current is TopLevel)
            {
                break;
            }
        }

        return false;
    }

    private static bool IsReadOnly(Control target)
        => target switch
        {
            TextBox textBox => textBox.IsReadOnly,
            NumericUpDown numeric => numeric.IsReadOnly,
            _ => false
        };

    private static InputProfile ResolveProfile(Control target)
    {
        var profileId = TouchKeyboardInput.GetProfile(target);
        if (!string.IsNullOrWhiteSpace(profileId)
            && InputProfileRegistry.TryGet(profileId, out var explicitProfile)
            && explicitProfile is not null)
        {
            return explicitProfile;
        }

        if (target is CalendarDatePicker datePicker)
        {
            var format = datePicker.CustomDateFormatString;
            return !string.IsNullOrWhiteSpace(format) && format.IndexOf('.') >= 0
                ? InputProfiles.DateDot
                : InputProfiles.DateSlash;
        }

        if (target is NumericUpDown numeric)
        {
            return numeric.Minimum >= 0 ? InputProfiles.PositiveDecimal : InputProfiles.Decimal;
        }

        if (target is TextBox textBox && textBox.PasswordChar != '\0')
        {
            return InputProfiles.Password;
        }

        return InputProfiles.Text;
    }

    private static IEditorAdapter? CreateAdapter(Control target, InputProfile profile)
    {
        if (TouchKeyboardAdapterRegistry.TryCreate(target, profile, out var custom) && custom is not null)
        {
            return custom;
        }

        return target switch
        {
            TextBox textBox => new TextBoxAdapter(textBox, profile),
            NumericUpDown numeric => new NumericUpDownAdapter(numeric, profile),
            CalendarDatePicker datePicker => new CalendarDatePickerAdapter(datePicker, profile),
            _ => null
        };
    }
}
