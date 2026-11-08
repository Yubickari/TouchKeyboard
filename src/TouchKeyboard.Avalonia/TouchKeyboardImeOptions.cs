using System;
using TouchKeyboard.Core;

namespace TouchKeyboard.Avalonia;

/// <summary>
/// Host configuration for the optional managed IME. The engine comes from a
/// language pack; the Avalonia frontend itself never references one.
/// </summary>
public sealed class TouchKeyboardImeOptions
{
    /// <summary>
    /// The language-pack engine, or <c>null</c> when the optional IME is not
    /// installed. A <c>null</c> engine means no IME key and no candidate row.
    /// </summary>
    public IImeEngine? Engine { get; set; }

    /// <summary>
    /// Initial Chinese/English state. <c>null</c> means Chinese when an engine and
    /// an IME-capable profile are present; <c>false</c> starts in English.
    /// </summary>
    public bool? DefaultEnabled { get; set; }

    /// <summary>Whether to remember the last 中/英 choice for later fields in this app session.</summary>
    public bool RememberMode { get; set; } = true;

    /// <summary>Candidates per page; clamped to 1–9. Default 9.</summary>
    public int CandidatePageSize { get; set; } = 9;

    /// <summary>Maximum raw pinyin length; clamped to 8–64. Default 32.</summary>
    public int MaxCompositionLength { get; set; } = 32;

    /// <summary>Whether Chinese mode maps common punctuation to full-width forms.</summary>
    public bool FullWidthPunctuation { get; set; } = true;
}
