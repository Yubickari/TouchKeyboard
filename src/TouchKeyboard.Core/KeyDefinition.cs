using System;

namespace TouchKeyboard.Core;

/// <summary>
/// One key in a <see cref="KeyboardLayout"/>. Widths are unitless; the frontend
/// scales a row to its available width. No pixel, color or font values live here.
/// </summary>
public sealed class KeyDefinition
{
    /// <summary>Creates a key definition.</summary>
    public KeyDefinition(
        string id,
        KeyLabel label,
        KeyCommand command,
        double widthUnits = 1.0,
        KeyStyle style = KeyStyle.Default,
        bool repeatable = false,
        KeyLabel? shiftedLabel = null,
        KeyCommand? shiftedCommand = null,
        int columnSpan = 1,
        int rowSpan = 1)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("A key id is required.", nameof(id));
        }

        if (widthUnits <= 0 || double.IsNaN(widthUnits) || double.IsInfinity(widthUnits))
        {
            throw new ArgumentOutOfRangeException(nameof(widthUnits), "Width must be finite and positive.");
        }

        if (columnSpan < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(columnSpan));
        }

        if (rowSpan < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(rowSpan));
        }

        Id = id;
        Label = label;
        Command = command;
        WidthUnits = widthUnits;
        Style = style;
        Repeatable = repeatable;
        ShiftedLabel = shiftedLabel;
        ShiftedCommand = shiftedCommand;
        ColumnSpan = columnSpan;
        RowSpan = rowSpan;
    }

    /// <summary>Stable key id, for example <c>key.a</c> or <c>key.backspace</c>.</summary>
    public string Id { get; }

    /// <summary>Primary cap label.</summary>
    public KeyLabel Label { get; }

    /// <summary>Optional shifted cap label.</summary>
    public KeyLabel? ShiftedLabel { get; }

    /// <summary>The command used when shift is inactive.</summary>
    public KeyCommand Command { get; }

    /// <summary>Optional command used when shift is active.</summary>
    public KeyCommand? ShiftedCommand { get; }

    /// <summary>Width in units. Named keys may be wider than character keys.</summary>
    public double WidthUnits { get; }

    /// <summary>Number of grid columns occupied; normally 1.</summary>
    public int ColumnSpan { get; }

    /// <summary>Number of grid rows occupied; normally 1.</summary>
    public int RowSpan { get; }

    /// <summary>Visual role.</summary>
    public KeyStyle Style { get; }

    /// <summary>Whether holding the key repeats its command (Backspace, arrows).</summary>
    public bool Repeatable { get; }

    /// <summary>Creates an empty cell that keeps a grid aligned.</summary>
    public static KeyDefinition Spacer(string id, double widthUnits = 1.0)
        => new KeyDefinition(id, KeyLabel.Literal(string.Empty), KeyCommand.None, widthUnits, KeyStyle.Spacer);
}
