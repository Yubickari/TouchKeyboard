using System.Collections.Generic;

namespace TouchKeyboard.Core;

/// <summary>
/// An immutable snapshot of the optional IME state: enablement, the raw
/// composition input, the current candidate page and its selection. Frontends
/// render this snapshot and never construct or mutate it.
/// </summary>
public sealed class ImeCompositionState
{
    internal ImeCompositionState(
        bool isEnabled,
        bool isComposing,
        string input,
        IReadOnlyList<ImeCandidate> candidates,
        int selectedIndex,
        int pageIndex,
        int pageCount,
        int candidateCount,
        int pageSize)
    {
        IsEnabled = isEnabled;
        IsComposing = isComposing;
        Input = input;
        Candidates = candidates;
        SelectedIndex = selectedIndex;
        PageIndex = pageIndex;
        PageCount = pageCount;
        CandidateCount = candidateCount;
        PageSize = pageSize;
    }

    /// <summary>Whether Chinese input is active; <c>false</c> means English is written directly.</summary>
    public bool IsEnabled { get; }

    /// <summary>Whether there is uncommitted pinyin input.</summary>
    public bool IsComposing { get; }

    /// <summary>The raw pinyin, lower-case; empty when not composing.</summary>
    public string Input { get; }

    /// <summary>The candidates on the current page.</summary>
    public IReadOnlyList<ImeCandidate> Candidates { get; }

    /// <summary>Zero-based selection on the current page; <c>-1</c> when there are no candidates.</summary>
    public int SelectedIndex { get; }

    /// <summary>Zero-based current page.</summary>
    public int PageIndex { get; }

    /// <summary>Number of candidate pages; <c>0</c> when there are no candidates.</summary>
    public int PageCount { get; }

    /// <summary>Total number of candidates across all pages.</summary>
    public int CandidateCount { get; }

    /// <summary>The current candidates per page (adaptive in the Avalonia frontend).</summary>
    public int PageSize { get; }

    /// <summary>Whether a previous page exists.</summary>
    public bool HasPreviousPage => PageIndex > 0;

    /// <summary>Whether a next page exists.</summary>
    public bool HasNextPage => PageIndex + 1 < PageCount;
}
