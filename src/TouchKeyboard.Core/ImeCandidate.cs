using System;

namespace TouchKeyboard.Core;

/// <summary>
/// One candidate returned by an <see cref="IImeEngine"/>. It exposes display text
/// only; ranking and scoring stay inside the engine.
/// </summary>
public sealed class ImeCandidate
{
    /// <summary>Creates a candidate.</summary>
    public ImeCandidate(string text)
    {
        if (text is null)
        {
            throw new ArgumentNullException(nameof(text));
        }

        Text = text;
    }

    /// <summary>The text to display and commit.</summary>
    public string Text { get; }

    /// <inheritdoc />
    public override string ToString() => Text;
}
