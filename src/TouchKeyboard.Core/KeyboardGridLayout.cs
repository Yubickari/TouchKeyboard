using System;
using System.Collections.Generic;

namespace TouchKeyboard.Core;

/// <summary>
/// A fixed columns × rows cell surface. Every cell holds a <see cref="KeyDefinition"/>
/// whose <see cref="KeyDefinition.ColumnSpan"/> and <see cref="KeyDefinition.RowSpan"/>
/// may cover several cells, which lets a Numpad widen 0 into an empty decimal slot
/// or stretch Confirm into an empty right-row2 slot. Cells that no key occupies stay
/// empty; they are not disabled keys.
/// </summary>
public sealed class KeyboardGridLayout
{
    /// <summary>Creates a grid after checking every cell fits and no cells overlap.</summary>
    public KeyboardGridLayout(int columns, int rows, IReadOnlyList<KeyboardGridCell> cells)
    {
        if (columns < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(columns), "A grid needs at least one column.");
        }

        if (rows < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(rows), "A grid needs at least one row.");
        }

        if (cells is null)
        {
            throw new ArgumentNullException(nameof(cells));
        }

        if (cells.Count == 0)
        {
            throw new ArgumentException("A grid must contain at least one cell.", nameof(cells));
        }

        var copy = new List<KeyboardGridCell>(cells.Count);
        var occupied = new bool[rows, columns];
        foreach (var cell in cells)
        {
            if (cell is null)
            {
                throw new ArgumentException("A grid cannot contain a null cell.", nameof(cells));
            }

            var lastRow = cell.Row + cell.RowSpan;
            var lastColumn = cell.Column + cell.ColumnSpan;
            if (lastRow > rows || lastColumn > columns)
            {
                throw new ArgumentException(
                    $"Key '{cell.Key.Id}' at ({cell.Row},{cell.Column}) spans beyond the {rows}x{columns} grid.",
                    nameof(cells));
            }

            for (var row = cell.Row; row < lastRow; row++)
            {
                for (var column = cell.Column; column < lastColumn; column++)
                {
                    if (occupied[row, column])
                    {
                        throw new ArgumentException(
                            $"Key '{cell.Key.Id}' at ({cell.Row},{cell.Column}) overlaps another cell.",
                            nameof(cells));
                    }

                    occupied[row, column] = true;
                }
            }

            copy.Add(cell);
        }

        Columns = columns;
        Rows = rows;
        Cells = copy.AsReadOnly();
    }

    /// <summary>Number of columns.</summary>
    public int Columns { get; }

    /// <summary>Number of rows.</summary>
    public int Rows { get; }

    /// <summary>The positioned cells, in the order they were supplied.</summary>
    public IReadOnlyList<KeyboardGridCell> Cells { get; }
}
