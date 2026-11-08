using System;
using System.Collections.Generic;

namespace TouchKeyboard.Core;

/// <summary>
/// The Core-owned IME state machine. It holds the raw pinyin, the candidate list,
/// the current page and selection; committing a candidate is performed by
/// <see cref="KeyboardSession"/> through its normal insert path so validation and
/// maximum lengths still apply.
/// </summary>
internal sealed class ImeComposition
{
    internal const int MaxCandidateCount = 100;
    internal const int MinPageSize = 1;
    internal const int MaxPageSize = 9;
    internal const int MinInputLengthLimit = 8;
    internal const int MaxInputLengthLimit = 64;

    private static readonly IReadOnlyList<ImeCandidate> NoCandidates = Array.AsReadOnly(Array.Empty<ImeCandidate>());

    private readonly IImeEngine _engine;
    private int _pageSize;
    private readonly int _maxInputLength;
    private readonly List<ImeCandidate> _candidates = new List<ImeCandidate>();
    private string _input = string.Empty;
    private int _pageIndex;
    private int _selectedIndex = -1;

    internal ImeComposition(
        IImeEngine engine,
        bool enabled,
        int pageSize,
        int maxInputLength)
    {
        _engine = engine ?? throw new ArgumentNullException(nameof(engine));
        IsEnabled = enabled;
        _pageSize = pageSize < MinPageSize ? MinPageSize : pageSize > MaxPageSize ? MaxPageSize : pageSize;
        _maxInputLength = maxInputLength < MinInputLengthLimit
            ? MinInputLengthLimit
            : maxInputLength > MaxInputLengthLimit
                ? MaxInputLengthLimit
                : maxInputLength;
    }

    public bool IsEnabled { get; private set; }

    public bool IsComposing => _input.Length > 0;

    public string Input => _input;

    public int PageSize => _pageSize;

    /// <summary>Changes the page size, resetting the current page and selection.</summary>
    public bool SetPageSize(int pageSize)
    {
        var clamped = pageSize < MinPageSize ? MinPageSize : pageSize > MaxPageSize ? MaxPageSize : pageSize;
        if (clamped == _pageSize)
        {
            return false;
        }

        _pageSize = clamped;
        _pageIndex = 0;
        _selectedIndex = _candidates.Count > 0 ? 0 : -1;
        return true;
    }

    public int MaxInputLength => _maxInputLength;

    public int CandidateCount => _candidates.Count;

    public int PageCount => _candidates.Count == 0
        ? 0
        : (_candidates.Count + _pageSize - 1) / _pageSize;

    public int PageIndex => _pageIndex;

    public int SelectedIndex => _candidates.Count == 0 ? -1 : _selectedIndex;

    public bool IsAtMaxInputLength => _input.Length >= _maxInputLength;

    /// <summary>Toggles Chinese/English input; a composition must already be committed or cleared.</summary>
    public void ToggleEnabled()
    {
        IsEnabled = !IsEnabled;
    }

    /// <summary>Maps a language punctuation character through the configured engine.</summary>
    public bool TryMapPunctuation(char input, out string mapped)
    {
        if (!IsEnabled)
        {
            mapped = string.Empty;
            return false;
        }

        return _engine.TryMapPunctuation(input, out mapped);
    }

    /// <summary>Appends one lower-case ASCII letter and refreshes the candidates.</summary>
    /// <returns><c>false</c> when the composition is at its length limit.</returns>
    public bool AppendLetter(char letter)
    {
        if (!IsEnabled || letter < 'a' || letter > 'z')
        {
            return false;
        }

        if (_input.Length >= _maxInputLength)
        {
            return false;
        }

        _input += letter;
        Refresh();
        return true;
    }

    /// <summary>Removes the last pinyin letter; an empty input ends the composition.</summary>
    public bool RemoveLastLetter()
    {
        if (_input.Length == 0)
        {
            return false;
        }

        _input = _input.Substring(0, _input.Length - 1);
        Refresh();
        return true;
    }

    /// <summary>Clears the composition input and candidates, keeping the enablement state.</summary>
    public void Reset()
    {
        _input = string.Empty;
        _candidates.Clear();
        _pageIndex = 0;
        _selectedIndex = -1;
    }

    /// <summary>
    /// Moves the selection within the whole candidate list, wrapping across pages.
    /// </summary>
    public bool MoveSelection(int delta)
    {
        if (_candidates.Count == 0 || delta == 0)
        {
            return false;
        }

        var current = _pageIndex * _pageSize + (_selectedIndex < 0 ? 0 : _selectedIndex);
        var next = (current + delta) % _candidates.Count;
        if (next < 0)
        {
            next += _candidates.Count;
        }

        _pageIndex = next / _pageSize;
        _selectedIndex = next % _pageSize;
        return true;
    }

    /// <summary>Moves one candidate page without wrapping; the selection resets to the first item.</summary>
    public bool ChangePage(int delta)
    {
        var pageCount = PageCount;
        if (pageCount <= 0 || delta == 0)
        {
            return false;
        }

        var next = _pageIndex + delta;
        if (next < 0)
        {
            next = 0;
        }
        else if (next >= pageCount)
        {
            next = pageCount - 1;
        }

        if (next == _pageIndex)
        {
            return false;
        }

        _pageIndex = next;
        _selectedIndex = 0;
        return true;
    }

    /// <summary>Selects a candidate by its index on the current page.</summary>
    public bool TrySelectFromCurrentPage(int index, out string text)
    {
        text = string.Empty;
        if (index < 0 || index >= CurrentPageCount())
        {
            return false;
        }

        _selectedIndex = index;
        text = _candidates[_pageIndex * _pageSize + index].Text;
        return true;
    }

    /// <summary>The text committed by Space/Enter: the selected candidate, or the raw input.</summary>
    public string CommitText
    {
        get
        {
            if (_candidates.Count > 0)
            {
                var index = _pageIndex * _pageSize + Math.Max(0, _selectedIndex);
                if (index >= 0 && index < _candidates.Count)
                {
                    return _candidates[index].Text;
                }
            }

            return _input;
        }
    }

    public ImeCompositionState Snapshot()
    {
        var page = new List<ImeCandidate>(_pageSize);
        var first = _pageIndex * _pageSize;
        for (var i = 0; i < _pageSize && first + i < _candidates.Count; i++)
        {
            page.Add(_candidates[first + i]);
        }

        IReadOnlyList<ImeCandidate> candidates = page.Count == 0
            ? NoCandidates
            : page.AsReadOnly();

        return new ImeCompositionState(
            IsEnabled,
            IsComposing,
            _input,
            candidates,
            SelectedIndex,
            _pageIndex,
            PageCount,
            _candidates.Count,
            _pageSize);
    }

    private int CurrentPageCount()
    {
        var remaining = _candidates.Count - _pageIndex * _pageSize;
        return remaining <= 0 ? 0 : Math.Min(_pageSize, remaining);
    }

    private void Refresh()
    {
        _candidates.Clear();
        _pageIndex = 0;
        _selectedIndex = -1;

        if (_input.Length == 0)
        {
            return;
        }

        if (_engine.TryQuery(_input, MaxCandidateCount, out var candidates)
            && candidates is not null)
        {
            for (var i = 0; i < candidates.Count && _candidates.Count < MaxCandidateCount; i++)
            {
                if (candidates[i] is null || string.IsNullOrEmpty(candidates[i].Text))
                {
                    continue;
                }

                _candidates.Add(candidates[i]);
            }
        }

        if (_candidates.Count > 0)
        {
            _selectedIndex = 0;
        }
    }
}
