using System;
using System.Collections.Generic;

namespace TouchKeyboard.Core;

/// <summary>
/// One horizontal row of keys. Each row scales independently to the available
/// width, so a 13-key QWERTY row and a 15-unit number row can share a panel.
/// </summary>
public sealed class KeyboardRow
{
    /// <summary>Creates a row from keys.</summary>
    public KeyboardRow(IReadOnlyList<KeyDefinition> keys)
    {
        if (keys is null)
        {
            throw new ArgumentNullException(nameof(keys));
        }

        if (keys.Count == 0)
        {
            throw new ArgumentException("A row must contain at least one key.", nameof(keys));
        }

        var copy = new List<KeyDefinition>(keys.Count);
        double total = 0;
        foreach (var key in keys)
        {
            if (key is null)
            {
                throw new ArgumentException("A row cannot contain a null key.", nameof(keys));
            }

            copy.Add(key);
            total += key.WidthUnits;
        }

        Keys = copy.AsReadOnly();
        TotalUnits = total;
    }

    /// <summary>The keys, left to right.</summary>
    public IReadOnlyList<KeyDefinition> Keys { get; }

    /// <summary>Sum of all key widths in this row.</summary>
    public double TotalUnits { get; }
}
