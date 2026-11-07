using System;
using System.Collections.Generic;

namespace TouchKeyboard.Core;

/// <summary>
/// An immutable keyboard surface: an id, a mode and either rows of keys or a
/// cell grid with column/row spans. The frontend renders it; the core never
/// knows pixels, brushes or fonts.
/// </summary>
public sealed class KeyboardLayout
{
    private static readonly KeyboardRow[] NoRows = new KeyboardRow[0];

    /// <summary>Creates a row-based layout.</summary>
    public KeyboardLayout(string id, KeyboardMode mode, IReadOnlyList<KeyboardRow> rows)
        : this(id, mode, rows, null)
    {
    }

    /// <summary>Creates a grid-based layout for surfaces that need spans.</summary>
    public KeyboardLayout(string id, KeyboardMode mode, KeyboardGridLayout grid)
        : this(id, mode, null, grid)
    {
    }

    private KeyboardLayout(
        string id,
        KeyboardMode mode,
        IReadOnlyList<KeyboardRow>? rows,
        KeyboardGridLayout? grid)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("A layout id is required.", nameof(id));
        }

        if (rows is null && grid is null)
        {
            throw new ArgumentNullException(nameof(rows), "A layout needs rows or a grid.");
        }

        IReadOnlyList<KeyboardRow> copy = Array.AsReadOnly(NoRows);
        if (rows is not null)
        {
            if (rows.Count == 0)
            {
                throw new ArgumentException("A layout must contain at least one row.", nameof(rows));
            }

            var list = new List<KeyboardRow>(rows.Count);
            foreach (var row in rows)
            {
                if (row is null)
                {
                    throw new ArgumentException("A layout cannot contain a null row.", nameof(rows));
                }

                list.Add(row);
            }

            copy = list.AsReadOnly();
        }

        Id = id;
        Mode = mode;
        Rows = copy;
        Grid = grid;
    }

    /// <summary>Stable layout id, for example <c>full-ansi-style</c>.</summary>
    public string Id { get; }

    /// <summary>The mode this layout renders.</summary>
    public KeyboardMode Mode { get; }

    /// <summary>
    /// The rows, top to bottom. Empty when the layout is grid-based; use
    /// <see cref="Grid"/> in that case.
    /// </summary>
    public IReadOnlyList<KeyboardRow> Rows { get; }

    /// <summary>The cell grid, or null when the layout is row-based.</summary>
    public KeyboardGridLayout? Grid { get; }

    /// <summary>Whether the surface is a cell grid rather than rows of keys.</summary>
    public bool IsGrid => Grid is not null;

    /// <summary>Number of visual rows: the grid row count or the row list count.</summary>
    public int RowCount => Grid?.Rows ?? Rows.Count;
}
