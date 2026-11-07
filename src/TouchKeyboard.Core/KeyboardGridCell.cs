using System;

namespace TouchKeyboard.Core;

/// <summary>
/// One positioned cell of a <see cref="KeyboardGridLayout"/>: a zero-based row
/// and column plus the key whose origin is that cell. The key's
/// <see cref="KeyDefinition.ColumnSpan"/> and <see cref="KeyDefinition.RowSpan"/>
/// decide how many cells it covers.
/// </summary>
public sealed class KeyboardGridCell
{
    /// <summary>Creates a cell at a row and column.</summary>
    public KeyboardGridCell(int row, int column, KeyDefinition key)
    {
        if (row < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(row), "The row cannot be negative.");
        }

        if (column < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(column), "The column cannot be negative.");
        }

        Row = row;
        Column = column;
        Key = key ?? throw new ArgumentNullException(nameof(key));
    }

    /// <summary>The zero-based row of the cell's origin.</summary>
    public int Row { get; }

    /// <summary>The zero-based column of the cell's origin.</summary>
    public int Column { get; }

    /// <summary>The key placed at the origin.</summary>
    public KeyDefinition Key { get; }

    /// <summary>Number of columns the key covers.</summary>
    public int ColumnSpan => Key.ColumnSpan;

    /// <summary>Number of rows the key covers.</summary>
    public int RowSpan => Key.RowSpan;
}
