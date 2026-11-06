using System;
using System.Collections.Generic;

namespace TouchKeyboard.Core;

/// <summary>
/// An immutable keyboard surface: an id, a mode and rows of keys. The frontend
/// renders it; the core never knows pixels, brushes or fonts.
/// </summary>
public sealed class KeyboardLayout
{
    /// <summary>Creates a layout.</summary>
    public KeyboardLayout(string id, KeyboardMode mode, IReadOnlyList<KeyboardRow> rows)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("A layout id is required.", nameof(id));
        }

        if (rows is null)
        {
            throw new ArgumentNullException(nameof(rows));
        }

        if (rows.Count == 0)
        {
            throw new ArgumentException("A layout must contain at least one row.", nameof(rows));
        }

        var copy = new List<KeyboardRow>(rows.Count);
        foreach (var row in rows)
        {
            if (row is null)
            {
                throw new ArgumentException("A layout cannot contain a null row.", nameof(rows));
            }

            copy.Add(row);
        }

        Id = id;
        Mode = mode;
        Rows = copy.AsReadOnly();
    }

    /// <summary>Stable layout id, for example <c>full-phone-hybrid</c>.</summary>
    public string Id { get; }

    /// <summary>The mode this layout renders.</summary>
    public KeyboardMode Mode { get; }

    /// <summary>The rows, top to bottom.</summary>
    public IReadOnlyList<KeyboardRow> Rows { get; }
}
