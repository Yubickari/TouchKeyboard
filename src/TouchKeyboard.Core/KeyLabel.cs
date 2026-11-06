using System;

namespace TouchKeyboard.Core;

/// <summary>
/// A key cap label. It is either literal text (letters, digits, symbols) or a
/// localization resource key resolved by the frontend for named keys.
/// </summary>
public readonly struct KeyLabel : IEquatable<KeyLabel>
{
    private KeyLabel(string? text, string? resourceKey)
    {
        Text = text;
        ResourceKey = resourceKey;
    }

    /// <summary>The literal text, when this is a literal label.</summary>
    public string? Text { get; }

    /// <summary>The resource key, when this is a named key.</summary>
    public string? ResourceKey { get; }

    /// <summary>Whether the label must be resolved through the frontend localizer.</summary>
    public bool IsResource => !string.IsNullOrEmpty(ResourceKey);

    /// <summary>Creates a literal label.</summary>
    public static KeyLabel Literal(string text)
    {
        if (text is null)
        {
            throw new ArgumentNullException(nameof(text));
        }

        return new KeyLabel(text, null);
    }

    /// <summary>Creates a label resolved from a localization resource key.</summary>
    public static KeyLabel Resource(string resourceKey)
    {
        if (string.IsNullOrWhiteSpace(resourceKey))
        {
            throw new ArgumentException("A resource key is required.", nameof(resourceKey));
        }

        return new KeyLabel(null, resourceKey);
    }

    /// <inheritdoc />
    public bool Equals(KeyLabel other)
        => string.Equals(Text, other.Text, StringComparison.Ordinal)
           && string.Equals(ResourceKey, other.ResourceKey, StringComparison.Ordinal);

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is KeyLabel other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode()
    {
        unchecked
        {
            return ((Text?.GetHashCode() ?? 0) * 397) ^ (ResourceKey?.GetHashCode() ?? 0);
        }
    }

    /// <summary>Equality operator.</summary>
    public static bool operator ==(KeyLabel left, KeyLabel right) => left.Equals(right);

    /// <summary>Inequality operator.</summary>
    public static bool operator !=(KeyLabel left, KeyLabel right) => !left.Equals(right);

    /// <inheritdoc />
    public override string ToString() => ResourceKey ?? Text ?? string.Empty;
}
