namespace TouchKeyboard.Core;

/// <summary>
/// The language-pack contract for the optional managed IME. An engine turns a
/// romanized composition string into ranked candidate text and maps language
/// punctuation; it contains no UI or platform code.
/// </summary>
public interface IImeEngine
{
    /// <summary>BCP-47 language tag, for example <c>zh-Hans</c>.</summary>
    string LanguageTag { get; }

    /// <summary>
    /// Returns up to <paramref name="maxCandidates"/> candidates for
    /// <paramref name="input"/>. Returns <c>false</c> when there are no candidates.
    /// </summary>
    bool TryQuery(
        string input,
        int maxCandidates,
        out IReadOnlyList<ImeCandidate> candidates);

    /// <summary>
    /// Maps language-specific punctuation to its target text. Returns <c>false</c>
    /// to keep the character unchanged.
    /// </summary>
    bool TryMapPunctuation(char input, out string mapped);
}
