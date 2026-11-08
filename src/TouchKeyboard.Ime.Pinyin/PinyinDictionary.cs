using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Text;
using System.Threading;
using TouchKeyboard.Core;

namespace TouchKeyboard.Ime.Pinyin;

/// <summary>
/// One lexicon entry: display text, tone-free syllable reading, frequency and
/// precomputed indexes. Immutable after load.
/// </summary>
internal sealed class PinyinEntry
{
    private string? _initials;

    internal PinyinEntry(string text, string pinyin, ushort frequency)
    {
        Text = text;
        Pinyin = pinyin;
        Frequency = frequency;
        Joined = pinyin.IndexOf('\'') < 0 ? pinyin : pinyin.Replace("'", string.Empty);
    }

    internal string Text { get; }

    internal string Pinyin { get; }

    internal string Joined { get; }

    internal ushort Frequency { get; }

    internal string Initials => _initials ??= BuildInitials();

    private string BuildInitials()
    {
        var builder = new StringBuilder(4);
        builder.Append(Pinyin[0]);
        for (var i = 1; i < Pinyin.Length; i++)
        {
            if (Pinyin[i - 1] == '\'')
            {
                builder.Append(Pinyin[i]);
            }
        }

        return builder.ToString();
    }
}

/// <summary>
/// A read-only pinyin lexicon loaded from an embedded, deflate-compressed binary
/// resource. Loading validates the stream and builds the prefix and exact-match
/// indexes; queries never mutate the dictionary.
/// </summary>
internal sealed class PinyinDictionary
{
    private const int MaxEntryCount = 2_000_000;
    private static readonly byte[] Magic = Encoding.ASCII.GetBytes("TKPINYIN\x01");

    private readonly PinyinEntry[] _byJoined;
    private readonly Lazy<PinyinEntry[]> _byInitials;
    private readonly Lazy<SentenceIndex> _sentenceIndex;

    private PinyinDictionary(PinyinEntry[] entries)
    {
        _byJoined = entries;
        Array.Sort(_byJoined, CompareJoined);
        _byInitials = new Lazy<PinyinEntry[]>(BuildInitialsIndex, LazyThreadSafetyMode.ExecutionAndPublication);
        _sentenceIndex = new Lazy<SentenceIndex>(BuildSentenceIndex, LazyThreadSafetyMode.ExecutionAndPublication);
    }

    internal int Count => _byJoined.Length;

    internal int SyllableCount => _sentenceIndex.Value.Syllables.Count;

    internal static PinyinDictionary Load(Assembly assembly, string resourceName)
    {
        if (assembly is null)
        {
            throw new ArgumentNullException(nameof(assembly));
        }

        if (string.IsNullOrWhiteSpace(resourceName))
        {
            throw new ArgumentException("A dictionary resource name is required.", nameof(resourceName));
        }

        using var compressed = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidDataException($"The embedded pinyin dictionary '{resourceName}' is missing.");

        // Decompress into memory first. BinaryReader reads the 7-bit length prefix and
        // short strings one small read at a time; doing that directly against
        // DeflateStream is much slower on some platforms (notably Windows). The
        // decompressed lexicon is only a few megabytes, so buffering is cheap.
        using var buffer = new MemoryStream();
        using (var deflate = new DeflateStream(compressed, CompressionMode.Decompress))
        {
            deflate.CopyTo(buffer);
        }

        buffer.Position = 0;
        using var reader = new BinaryReader(buffer, Encoding.UTF8);

        var magic = reader.ReadBytes(Magic.Length);
        if (magic.Length != Magic.Length)
        {
            throw new InvalidDataException("The pinyin dictionary header is truncated.");
        }

        for (var i = 0; i < Magic.Length; i++)
        {
            if (magic[i] != Magic[i])
            {
                throw new InvalidDataException("The pinyin dictionary header is not recognised.");
            }
        }

        var count = reader.ReadInt32();
        if (count <= 0 || count > MaxEntryCount)
        {
            throw new InvalidDataException($"The pinyin dictionary entry count {count} is not valid.");
        }

        var entries = new PinyinEntry[count];
        for (var i = 0; i < count; i++)
        {
            var text = ReadString(reader, Encoding.UTF8, "text");
            var pinyin = ReadString(reader, Encoding.ASCII, "reading");
            var frequency = reader.ReadUInt16();
            ValidatePinyin(pinyin);
            entries[i] = new PinyinEntry(text, pinyin, frequency);
        }

        return new PinyinDictionary(entries);
    }

    private PinyinEntry[] BuildInitialsIndex()
    {
        var copy = (PinyinEntry[])_byJoined.Clone();
        Array.Sort(copy, CompareInitials);
        return copy;
    }

    private SentenceIndex BuildSentenceIndex()
    {
        var exact = new Dictionary<string, List<PinyinEntry>>(StringComparer.Ordinal);
        var syllables = new HashSet<string>(StringComparer.Ordinal);
        var maxSyllableLength = 1;

        foreach (var entry in _byJoined)
        {
            if (!exact.TryGetValue(entry.Joined, out var list))
            {
                list = new List<PinyinEntry>();
                exact.Add(entry.Joined, list);
            }

            list.Add(entry);

            var start = 0;
            for (var i = 0; i <= entry.Pinyin.Length; i++)
            {
                if (i < entry.Pinyin.Length && entry.Pinyin[i] != '\'')
                {
                    continue;
                }

                var syllable = entry.Pinyin.Substring(start, i - start);
                syllables.Add(syllable);
                if (syllable.Length > maxSyllableLength)
                {
                    maxSyllableLength = syllable.Length;
                }

                start = i + 1;
            }
        }

        var arrays = new Dictionary<string, PinyinEntry[]>(exact.Count, StringComparer.Ordinal);
        foreach (var pair in exact)
        {
            var list = pair.Value;
            list.Sort(CompareEntry);
            arrays.Add(pair.Key, list.ToArray());
        }

        return new SentenceIndex(arrays, syllables, maxSyllableLength);
    }

    internal bool TryQuery(
        string input,
        int maxCandidates,
        out IReadOnlyList<ImeCandidate> candidates)
    {
        candidates = Array.Empty<ImeCandidate>();
        if (string.IsNullOrEmpty(input) || maxCandidates <= 0)
        {
            return false;
        }

        for (var i = 0; i < input.Length; i++)
        {
            var c = input[i];
            if (c < 'a' || c > 'z')
            {
                return false;
            }
        }

        var results = new Dictionary<string, ScoredCandidate>(StringComparer.Ordinal);
        var hadWord = CollectPrefix(_byJoined, input, exactScore: 10_000, prefixScore: 7_000, results, maxScan: 600, initials: false);

        if (input.Length >= 2)
        {
            hadWord |= CollectPrefix(_byInitials.Value, input, exactScore: 5_000, prefixScore: 3_000, results, maxScan: 400, initials: true);
        }

        // Sentence candidates are a fallback for input that is not a known word
        // or abbreviation; keeping them out of word queries avoids noise such as
        // "你哈哦" after the exact "你好" hit.
        if (!hadWord)
        {
            CollectSentences(input, results);
        }

        if (results.Count == 0)
        {
            return false;
        }

        var ordered = new List<ScoredCandidate>(results.Values);
        ordered.Sort(CompareScored);

        var take = Math.Min(maxCandidates, ordered.Count);
        var output = new ImeCandidate[take];
        for (var i = 0; i < take; i++)
        {
            output[i] = new ImeCandidate(ordered[i].Text);
        }

        candidates = output;
        return take > 0;
    }

    private bool CollectPrefix(
        PinyinEntry[] entries,
        string input,
        int exactScore,
        int prefixScore,
        Dictionary<string, ScoredCandidate> results,
        int maxScan,
        bool initials)
    {
        var added = false;
        var index = LowerBound(entries, input, initials);
        var scanned = 0;
        for (; index < entries.Length; index++)
        {
            var entry = entries[index];
            var key = initials ? entry.Initials : entry.Joined;
            if (!key.StartsWith(input, StringComparison.Ordinal))
            {
                break;
            }

            var score = string.Equals(key, input, StringComparison.Ordinal) ? exactScore : prefixScore;
            AddResult(results, entry, score);
            added = true;
            if (++scanned >= maxScan)
            {
                break;
            }
        }

        return added;
    }

    private void CollectSentences(string input, Dictionary<string, ScoredCandidate> results)
    {
        var index = _sentenceIndex.Value;
        var paths = FindSegmentations(input, index);
        foreach (var path in paths)
        {
            var sentences = BuildSentences(path, index);
            foreach (var sentence in sentences)
            {
                var score = 1_000 + (int)Math.Min(990, sentence.Score * 50);
                AddSentenceResult(results, sentence.Text, sentence.Frequency, score);
            }
        }
    }

    private static List<string[]> FindSegmentations(string input, SentenceIndex index)
    {
        var paths = new List<string[]>();
        var current = new List<string>();
        var visited = 0;
        Walk(0);
        return paths;

        void Walk(int start)
        {
            if (paths.Count >= 8 || visited >= 4_000)
            {
                return;
            }

            visited++;
            if (start == input.Length)
            {
                paths.Add(current.ToArray());
                return;
            }

            var remaining = input.Length - start;
            var maxLength = Math.Min(index.MaxSyllableLength, remaining);
            for (var length = 1; length <= maxLength; length++)
            {
                var syllable = input.Substring(start, length);
                if (!index.Syllables.Contains(syllable))
                {
                    continue;
                }

                current.Add(syllable);
                Walk(start + length);
                current.RemoveAt(current.Count - 1);
            }
        }
    }

    private static List<SentenceCandidate> BuildSentences(string[] syllables, SentenceIndex index)
    {
        var levels = new List<BeamState>?[syllables.Length + 1];
        levels[0] = new List<BeamState> { new BeamState(0, string.Empty, 0, 0) };

        for (var position = 0; position < syllables.Length; position++)
        {
            var level = levels[position];
            if (level is null || level.Count == 0)
            {
                continue;
            }

            if (level.Count > 64)
            {
                level.Sort(CompareBeam);
                level.RemoveRange(64, level.Count - 64);
            }

            var limit = Math.Min(level.Count, 16);
            for (var stateIndex = 0; stateIndex < limit; stateIndex++)
            {
                var state = level[stateIndex];
                if (state.Segments >= 4)
                {
                    continue;
                }

                var maxLength = Math.Min(4, syllables.Length - position);
                for (var length = 1; length <= maxLength; length++)
                {
                    var joined = JoinSyllables(syllables, position, length);
                    if (!index.Exact.TryGetValue(joined, out var entries))
                    {
                        continue;
                    }

                    var entryLimit = Math.Min(entries.Length, 4);
                    for (var entryIndex = 0; entryIndex < entryLimit; entryIndex++)
                    {
                        var entry = entries[entryIndex];
                        var next = new BeamState(
                            position + length,
                            state.Text + entry.Text,
                            state.Segments + 1,
                            state.Score + Math.Log(1 + entry.Frequency));
                        var target = levels[position + length];
                        if (target is null)
                        {
                            target = new List<BeamState>();
                            levels[position + length] = target;
                        }

                        target.Add(next);
                    }
                }
            }
        }

        var complete = levels[syllables.Length];
        if (complete is null || complete.Count == 0)
        {
            return new List<SentenceCandidate>();
        }

        complete.Sort(CompareBeam);
        var results = new List<SentenceCandidate>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var state in complete)
        {
            if (!seen.Add(state.Text))
            {
                continue;
            }

            var averageScore = state.Segments == 0 ? 0 : state.Score / state.Segments;
            results.Add(new SentenceCandidate(state.Text, (int)Math.Min(ushort.MaxValue, averageScore * 1000)));
            if (results.Count >= 6)
            {
                break;
            }
        }

        return results;
    }

    private static void AddResult(
        Dictionary<string, ScoredCandidate> results,
        PinyinEntry entry,
        int score)
    {
        if (results.TryGetValue(entry.Text, out var existing) && existing.Score >= score)
        {
            return;
        }

        results[entry.Text] = new ScoredCandidate(
            entry.Text,
            score,
            entry.Frequency,
            entry.Text.Length);
    }

    private static void AddSentenceResult(
        Dictionary<string, ScoredCandidate> results,
        string text,
        int frequency,
        int score)
    {
        if (results.TryGetValue(text, out var existing) && existing.Score >= score)
        {
            return;
        }

        results[text] = new ScoredCandidate(text, score, frequency, text.Length);
    }

    private static string ReadString(BinaryReader reader, Encoding encoding, string field)
    {
        var length = reader.ReadByte();
        if (length == 0)
        {
            throw new InvalidDataException($"The pinyin dictionary contains an empty {field}.");
        }

        var bytes = reader.ReadBytes(length);
        if (bytes.Length != length)
        {
            throw new InvalidDataException($"The pinyin dictionary {field} is truncated.");
        }

        return encoding.GetString(bytes);
    }

    private static void ValidatePinyin(string pinyin)
    {
        if (string.IsNullOrEmpty(pinyin))
        {
            throw new InvalidDataException("The pinyin dictionary contains an empty reading.");
        }

        var syllableLength = 0;
        for (var i = 0; i < pinyin.Length; i++)
        {
            var c = pinyin[i];
            if (c >= 'a' && c <= 'z')
            {
                syllableLength++;
                continue;
            }

            if (c == '\'' && syllableLength > 0)
            {
                syllableLength = 0;
                continue;
            }

            throw new InvalidDataException($"The pinyin reading '{pinyin}' is not valid.");
        }

        if (syllableLength == 0)
        {
            throw new InvalidDataException($"The pinyin reading '{pinyin}' is not valid.");
        }
    }

    private static int LowerBound(PinyinEntry[] entries, string input, bool initials)
    {
        var low = 0;
        var high = entries.Length;
        while (low < high)
        {
            var middle = low + ((high - low) >> 1);
            var key = initials ? entries[middle].Initials : entries[middle].Joined;
            if (string.CompareOrdinal(key, input) < 0)
            {
                low = middle + 1;
            }
            else
            {
                high = middle;
            }
        }

        return low;
    }

    private static string JoinSyllables(string[] syllables, int start, int length)
    {
        var builder = new StringBuilder();
        for (var i = 0; i < length; i++)
        {
            builder.Append(syllables[start + i]);
        }

        return builder.ToString();
    }

    private static int CompareJoined(PinyinEntry left, PinyinEntry right)
    {
        var result = string.CompareOrdinal(left.Joined, right.Joined);
        return result != 0 ? result : CompareEntry(left, right);
    }

    private static int CompareInitials(PinyinEntry left, PinyinEntry right)
    {
        var result = string.CompareOrdinal(left.Initials, right.Initials);
        return result != 0 ? result : CompareEntry(left, right);
    }

    private static int CompareEntry(PinyinEntry left, PinyinEntry right)
    {
        var result = right.Frequency.CompareTo(left.Frequency);
        if (result != 0)
        {
            return result;
        }

        result = right.Text.Length.CompareTo(left.Text.Length);
        return result != 0 ? result : string.CompareOrdinal(left.Text, right.Text);
    }

    private static int CompareScored(ScoredCandidate left, ScoredCandidate right)
    {
        var result = right.Score.CompareTo(left.Score);
        if (result != 0)
        {
            return result;
        }

        result = right.Frequency.CompareTo(left.Frequency);
        if (result != 0)
        {
            return result;
        }

        result = right.Length.CompareTo(left.Length);
        return result != 0 ? result : string.CompareOrdinal(left.Text, right.Text);
    }

    private static int CompareBeam(BeamState left, BeamState right)
    {
        var leftAverage = left.Segments == 0 ? 0 : left.Score / left.Segments;
        var rightAverage = right.Segments == 0 ? 0 : right.Score / right.Segments;
        var result = rightAverage.CompareTo(leftAverage);
        if (result != 0)
        {
            return result;
        }

        result = left.Segments.CompareTo(right.Segments);
        return result != 0 ? result : string.CompareOrdinal(left.Text, right.Text);
    }

    private sealed class SentenceIndex
    {
        internal SentenceIndex(
            Dictionary<string, PinyinEntry[]> exact,
            HashSet<string> syllables,
            int maxSyllableLength)
        {
            Exact = exact;
            Syllables = syllables;
            MaxSyllableLength = maxSyllableLength;
        }

        internal Dictionary<string, PinyinEntry[]> Exact { get; }

        internal HashSet<string> Syllables { get; }

        internal int MaxSyllableLength { get; }
    }

    private readonly struct ScoredCandidate
    {
        internal ScoredCandidate(string text, int score, int frequency, int length)
        {
            Text = text;
            Score = score;
            Frequency = frequency;
            Length = length;
        }

        internal string Text { get; }

        internal int Score { get; }

        internal int Frequency { get; }

        internal int Length { get; }
    }

    private readonly struct SentenceCandidate
    {
        internal SentenceCandidate(string text, int frequency)
        {
            Text = text;
            Frequency = frequency;
            Score = frequency / 1000.0;
        }

        internal string Text { get; }

        internal int Frequency { get; }

        internal double Score { get; }
    }

    private sealed class BeamState
    {
        internal BeamState(int position, string text, int segments, double score)
        {
            Position = position;
            Text = text;
            Segments = segments;
            Score = score;
        }

        internal int Position { get; }

        internal string Text { get; }

        internal int Segments { get; }

        internal double Score { get; }
    }
}
