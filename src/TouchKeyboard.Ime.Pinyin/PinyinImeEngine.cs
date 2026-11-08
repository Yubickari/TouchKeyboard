using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using TouchKeyboard.Core;

namespace TouchKeyboard.Ime.Pinyin;

/// <summary>
/// The managed pinyin engine shipped in <c>TouchKeyboard.Ime.Pinyin</c>. It
/// supports full pinyin, word/sentence segmentation and common initial-letter
/// abbreviations, and loads its embedded lexicon lazily on first query.
/// </summary>
/// <remarks>
/// The engine is immutable and thread-safe after construction: the lexicon is
/// loaded once and every query only reads it. Create one instance and share it
/// across sessions.
/// </remarks>
public class PinyinImeEngine : IImeEngine
{
    internal const string CommonDictionaryResource = "TouchKeyboard.Ime.Pinyin.Resources.pinyin-common.bin";

    private readonly Lazy<PinyinDictionary> _dictionary;

    /// <summary>Creates the engine with the common lexicon.</summary>
    public PinyinImeEngine()
        : this(typeof(PinyinImeEngine).GetTypeInfo().Assembly, CommonDictionaryResource)
    {
    }

    /// <summary>Creates an engine over another embedded lexicon; used by the extended pack.</summary>
    internal PinyinImeEngine(Assembly dictionaryAssembly, string dictionaryResourceName)
    {
        if (dictionaryAssembly is null)
        {
            throw new ArgumentNullException(nameof(dictionaryAssembly));
        }

        if (string.IsNullOrWhiteSpace(dictionaryResourceName))
        {
            throw new ArgumentException("A dictionary resource name is required.", nameof(dictionaryResourceName));
        }

        _dictionary = new Lazy<PinyinDictionary>(
            () => PinyinDictionary.Load(dictionaryAssembly, dictionaryResourceName),
            LazyThreadSafetyMode.ExecutionAndPublication);
    }

    /// <inheritdoc />
    public string LanguageTag => "zh-Hans";

    /// <inheritdoc />
    public bool TryQuery(
        string input,
        int maxCandidates,
        out IReadOnlyList<ImeCandidate> candidates)
    {
        if (input is null)
        {
            candidates = Array.Empty<ImeCandidate>();
            return false;
        }

        var normalized = input.ToLowerInvariant();
        return _dictionary.Value.TryQuery(normalized, maxCandidates, out candidates);
    }

    /// <inheritdoc />
    public bool TryMapPunctuation(char input, out string mapped)
    {
        switch (input)
        {
            case ',':
                mapped = "，";
                return true;
            case '.':
                mapped = "。";
                return true;
            case '?':
                mapped = "？";
                return true;
            case '!':
                mapped = "！";
                return true;
            case ':':
                mapped = "：";
                return true;
            case ';':
                mapped = "；";
                return true;
            case '(':
                mapped = "（";
                return true;
            case ')':
                mapped = "）";
                return true;
            case '[':
                mapped = "【";
                return true;
            case ']':
                mapped = "】";
                return true;
            case '{':
                mapped = "｛";
                return true;
            case '}':
                mapped = "｝";
                return true;
            case '<':
                mapped = "《";
                return true;
            case '>':
                mapped = "》";
                return true;
            default:
                mapped = string.Empty;
                return false;
        }
    }
}
