using System;

namespace TouchKeyboard.Core;

/// <summary>
/// An immutable snapshot of a <see cref="KeyboardSession"/> that a frontend can
/// render without reaching into mutable session fields.
/// </summary>
public sealed class KeyboardState
{
    /// <summary>Creates a state snapshot.</summary>
    public KeyboardState(
        string text,
        int caretIndex,
        KeyboardMode mode,
        bool isShiftActive,
        KeyboardLayout layout,
        FullKeyboardLayoutKind fullKeyboardLayout,
        NumericKeypadLayoutKind numericKeypadLayout,
        InputValidationError? error,
        bool isDirty,
        bool isCancelled)
    {
        Text = text;
        CaretIndex = caretIndex;
        Mode = mode;
        IsShiftActive = isShiftActive;
        Layout = layout;
        FullKeyboardLayout = fullKeyboardLayout;
        NumericKeypadLayout = numericKeypadLayout;
        Error = error;
        IsDirty = isDirty;
        IsCancelled = isCancelled;
    }

    /// <summary>The current buffer.</summary>
    public string Text { get; }

    /// <summary>The caret position, 0..Text.Length.</summary>
    public int CaretIndex { get; }

    /// <summary>The active mode.</summary>
    public KeyboardMode Mode { get; }

    /// <summary>Whether the sticky shift is active.</summary>
    public bool IsShiftActive { get; }

    /// <summary>The layout to render.</summary>
    public KeyboardLayout Layout { get; }

    /// <summary>The selected full-keyboard geometry.</summary>
    public FullKeyboardLayoutKind FullKeyboardLayout { get; }

    /// <summary>The selected general numeric surface.</summary>
    public NumericKeypadLayoutKind NumericKeypadLayout { get; }

    /// <summary>The last validation error, if any.</summary>
    public InputValidationError? Error { get; }

    /// <summary>Whether the buffer differs from the value captured on open.</summary>
    public bool IsDirty { get; }

    /// <summary>Whether the session was cancelled.</summary>
    public bool IsCancelled { get; }
}

/// <summary>Event data for <see cref="KeyboardSession.StateChanged"/>.</summary>
public sealed class KeyboardStateChangedEventArgs : EventArgs
{
    /// <summary>Creates the event data.</summary>
    public KeyboardStateChangedEventArgs(KeyboardState state)
    {
        State = state;
    }

    /// <summary>The new state snapshot.</summary>
    public KeyboardState State { get; }
}
