using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia.Controls;
using TouchKeyboard.Core;

namespace TouchKeyboard.Avalonia;

/// <summary>
/// Bridge between one frontend control and a keyboard session. Adapters read the
/// initial text and metadata and write the validated text back; they do not
/// validate the input themselves (the profile does).
/// </summary>
public interface IEditorAdapter
{
    /// <summary>The target control.</summary>
    Control Control { get; }

    /// <summary>The resolved input profile.</summary>
    InputProfile Profile { get; }

    /// <summary>Reads the current display text.</summary>
    string GetText();

    /// <summary>Target maximum length; 0 means unlimited.</summary>
    int MaxLength { get; }

    /// <summary>Whether the target accepts negative numbers.</summary>
    bool AllowsNegativeSign { get; }

    /// <summary>Optional numeric minimum.</summary>
    decimal? Minimum { get; }

    /// <summary>Optional numeric maximum.</summary>
    decimal? Maximum { get; }

    /// <summary>Writes the validated text back to the control.</summary>
    bool TryCommit(string text, out InputValidationError? error);
}

/// <summary>Custom adapter factory for controls the frontend owns.</summary>
public interface IEditorAdapterFactory
{
    /// <summary>Whether this factory handles the control.</summary>
    bool CanHandle(Control control);

    /// <summary>Creates the adapter.</summary>
    IEditorAdapter Create(Control control, InputProfile profile);
}

/// <summary>Registry for host-defined editor adapters.</summary>
public static class TouchKeyboardAdapterRegistry
{
    private static readonly List<IEditorAdapterFactory> Factories = new List<IEditorAdapterFactory>();

    /// <summary>Registers a custom adapter factory.</summary>
    public static void Register(IEditorAdapterFactory factory)
    {
        if (factory is null)
        {
            throw new ArgumentNullException(nameof(factory));
        }

        Factories.Add(factory);
    }

    /// <summary>Registers a typed factory.</summary>
    public static void Register<TControl>(Func<TControl, InputProfile, IEditorAdapter> factory)
        where TControl : Control
    {
        if (factory is null)
        {
            throw new ArgumentNullException(nameof(factory));
        }

        Factories.Add(new DelegateFactory<TControl>(factory));
    }

    internal static bool TryCreate(
        Control control,
        InputProfile profile,
        out IEditorAdapter? adapter)
    {
        foreach (var factory in Factories)
        {
            if (factory.CanHandle(control))
            {
                adapter = factory.Create(control, profile);
                return true;
            }
        }

        adapter = null;
        return false;
    }

    private sealed class DelegateFactory<TControl> : IEditorAdapterFactory
        where TControl : Control
    {
        private readonly Func<TControl, InputProfile, IEditorAdapter> _factory;

        public DelegateFactory(Func<TControl, InputProfile, IEditorAdapter> factory)
        {
            _factory = factory;
        }

        public bool CanHandle(Control control) => control is TControl;

        public IEditorAdapter Create(Control control, InputProfile profile)
            => _factory((TControl)control, profile);
    }
}

internal sealed class TextBoxAdapter : IEditorAdapter
{
    private readonly TextBox _textBox;

    public TextBoxAdapter(TextBox textBox, InputProfile profile)
    {
        _textBox = textBox;
        Profile = profile;
    }

    public Control Control => _textBox;

    public InputProfile Profile { get; }

    public string GetText() => _textBox.Text ?? string.Empty;

    public int MaxLength => _textBox.MaxLength;

    public bool AllowsNegativeSign => true;

    public decimal? Minimum => null;

    public decimal? Maximum => null;

    public bool TryCommit(string text, out InputValidationError? error)
    {
        error = null;
        _textBox.Text = text;
        return true;
    }
}

internal sealed class NumericUpDownAdapter : IEditorAdapter
{
    private readonly NumericUpDown _numeric;

    public NumericUpDownAdapter(NumericUpDown numeric, InputProfile profile)
    {
        _numeric = numeric;
        Profile = profile;
    }

    public Control Control => _numeric;

    public InputProfile Profile { get; }

    public string GetText()
        => _numeric.Value?.ToString("0.##########", CultureInfo.InvariantCulture) ?? string.Empty;

    public int MaxLength => 0;

    public bool AllowsNegativeSign => _numeric.Minimum < 0;

    public decimal? Minimum => _numeric.Minimum;

    public decimal? Maximum => _numeric.Maximum;

    public bool TryCommit(string text, out InputValidationError? error)
    {
        error = null;

        if (string.IsNullOrWhiteSpace(text))
        {
            _numeric.Value = null;
            return true;
        }

        if (!decimal.TryParse(
                text,
                NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint,
                CultureInfo.InvariantCulture,
                out var value))
        {
            error = new InputValidationError(InputValidationErrorCode.InvalidDecimal);
            return false;
        }

        if (_numeric.Minimum is { } minimum && value < minimum)
        {
            error = new InputValidationError(InputValidationErrorCode.ValueTooSmall, minimum);
            return false;
        }

        if (_numeric.Maximum is { } maximum && value > maximum)
        {
            error = new InputValidationError(InputValidationErrorCode.ValueTooLarge, maximum);
            return false;
        }

        _numeric.Value = value;
        return true;
    }
}

internal sealed class CalendarDatePickerAdapter : IEditorAdapter
{
    private readonly CalendarDatePicker _datePicker;

    public CalendarDatePickerAdapter(CalendarDatePicker datePicker, InputProfile profile)
    {
        _datePicker = datePicker;
        Profile = profile;
    }

    public Control Control => _datePicker;

    public InputProfile Profile { get; }

    public string GetText()
    {
        if (!string.IsNullOrWhiteSpace(_datePicker.Text))
        {
            return _datePicker.Text!;
        }

        return _datePicker.SelectedDate?.ToString("yyyy/MM/dd", CultureInfo.InvariantCulture)
               ?? string.Empty;
    }

    public int MaxLength => 0;

    public bool AllowsNegativeSign => false;

    public decimal? Minimum => null;

    public decimal? Maximum => null;

    public bool TryCommit(string text, out InputValidationError? error)
    {
        error = null;

        if (string.IsNullOrWhiteSpace(text))
        {
            _datePicker.SelectedDate = null;
            return true;
        }

        if (!DateTime.TryParseExact(
                text,
                "yyyy/MM/dd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var date))
        {
            error = new InputValidationError(InputValidationErrorCode.InvalidDate);
            return false;
        }

        _datePicker.SelectedDate = date;
        return true;
    }
}
