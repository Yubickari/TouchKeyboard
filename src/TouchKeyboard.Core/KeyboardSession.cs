using System;
using System.Globalization;

namespace TouchKeyboard.Core;

/// <summary>
/// Owns the edit buffer, caret, mode, full-keyboard page, shift state and
/// commit/cancel lifecycle for one field. Closing always discards; only
/// <see cref="TryCommit"/> produces a value for the host to write back.
/// </summary>
public sealed class KeyboardSession
{
    private readonly string _initialText;
    private readonly int _effectiveMaxLength;
    private readonly bool _allowsNegativeSign;
    private readonly decimal? _minimum;
    private readonly decimal? _maximum;
    private readonly FullKeyboardLayoutKind _fullKeyboardLayout;
    private readonly bool _imeFullWidthPunctuation;
    private NumericKeypadLayoutKind _numericKeypadLayout;

    private string _text;
    private int _caret;
    private KeyboardMode _mode;
    private FullKeyboardPage _page;
    private bool _shift;
    private InputValidationError? _error;
    private bool _cancelled;
    private KeyboardLayout _layout;
    private ImeComposition? _ime;

    /// <summary>Creates a session for one field.</summary>
    public KeyboardSession(KeyboardSessionOptions options)
    {
        if (options is null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        if (options.Profile is null)
        {
            throw new ArgumentNullException(nameof(options), "A profile is required.");
        }

        if (options.MaxLength < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(options), "MaxLength cannot be negative.");
        }

        if (options.Minimum.HasValue && options.Maximum.HasValue && options.Minimum.Value > options.Maximum.Value)
        {
            throw new ArgumentException("Minimum cannot be greater than Maximum.", nameof(options));
        }

        Profile = options.Profile;
        Label = options.Label;
        Unit = options.Unit;
        _allowsNegativeSign = options.AllowsNegativeSign;
        _minimum = options.Minimum;
        _maximum = options.Maximum;
        _fullKeyboardLayout = options.FullKeyboardLayout;
        _numericKeypadLayout = options.NumericKeypadLayout;
        _imeFullWidthPunctuation = options.ImeFullWidthPunctuation;
        _effectiveMaxLength = ResolveMaxLength(Profile.MaxLength, options.MaxLength);

        if (options.ImeEngine is not null && Profile.AllowsIme && Profile.AllowsFullKeyboard)
        {
            _ime = new ImeComposition(
                options.ImeEngine,
                options.ImeEnabled ?? true,
                options.ImeCandidatePageSize,
                options.ImeMaxCompositionLength);
        }

        _text = options.Text ?? string.Empty;
        _initialText = _text;
        _caret = _text.Length;
        _mode = Profile.DefaultMode;
        _page = FullKeyboardPage.Main;
        _shift = false;
        _error = null;
        _cancelled = false;
        _layout = BuildLayout();
        State = Snapshot();
    }

    /// <summary>The profile this session validates against.</summary>
    public InputProfile Profile { get; }

    /// <summary>The field description, if supplied.</summary>
    public string? Label { get; }

    /// <summary>The unit, if supplied.</summary>
    public string? Unit { get; }

    /// <summary>The current immutable snapshot.</summary>
    public KeyboardState State { get; private set; }

    /// <summary>Whether the session has been cancelled.</summary>
    public bool IsCancelled => _cancelled;

    /// <summary>Raised after every state change.</summary>
    public event EventHandler<KeyboardStateChangedEventArgs>? StateChanged;

    /// <summary>
    /// Executes an editing command. Submit and Close are host actions and are
    /// ignored here; the host calls <see cref="TryCommit"/> or <see cref="Cancel"/>.
    /// </summary>
    public void Execute(KeyCommand command)
    {
        if (_cancelled)
        {
            return;
        }

        switch (command.Kind)
        {
            case KeyCommandKind.None:
            case KeyCommandKind.Submit:
            case KeyCommandKind.Close:
                return;
            case KeyCommandKind.InsertText:
                HandleInsert(command.Text);
                return;
            case KeyCommandKind.Backspace:
                Backspace();
                return;
            case KeyCommandKind.Clear:
                Clear();
                return;
            case KeyCommandKind.MoveCaretLeft:
                if (!HandleImeSelectionMove(-1))
                {
                    MoveCaret(-1);
                }

                return;
            case KeyCommandKind.MoveCaretRight:
                if (!HandleImeSelectionMove(1))
                {
                    MoveCaret(1);
                }

                return;
            case KeyCommandKind.StepDigitUp:
                if (!HandleImeSelectionMove(-1))
                {
                    StepDigit(1);
                }

                return;
            case KeyCommandKind.StepDigitDown:
                if (!HandleImeSelectionMove(1))
                {
                    StepDigit(-1);
                }

                return;
            case KeyCommandKind.ToggleSign:
                ToggleSign();
                return;
            case KeyCommandKind.ToggleShift:
                if (!CommitCompositionForAction())
                {
                    return;
                }

                ToggleShift();
                return;
            case KeyCommandKind.SwitchToFullKeyboard:
                if (!CommitCompositionForAction())
                {
                    return;
                }

                SwitchToFullKeyboard();
                return;
            case KeyCommandKind.SwitchToNumericKeypad:
                if (!CommitCompositionForAction())
                {
                    return;
                }

                SwitchToNumericKeypad();
                return;
            case KeyCommandKind.SwitchToSymbolPage:
                if (!CommitCompositionForAction())
                {
                    return;
                }

                SwitchToSymbolPage();
                return;
            case KeyCommandKind.SwitchToMainPage:
                if (!CommitCompositionForAction())
                {
                    return;
                }

                SwitchToMainPage();
                return;
            case KeyCommandKind.ToggleIme:
                ToggleIme();
                return;
            default:
                return;
        }
    }

    /// <summary>Switches the general numeric surface; ignored for restricted numeric profiles.</summary>
    public void SetNumericKeypadLayout(NumericKeypadLayoutKind layout)
    {
        if (_cancelled || _mode != KeyboardMode.NumericKeypad || !Profile.AllowsFullKeyboard)
        {
            return;
        }

        if (_numericKeypadLayout == layout)
        {
            return;
        }

        _numericKeypadLayout = layout;
        _layout = BuildLayout();
        _error = null;
        Publish();
    }

    /// <summary>
    /// Selects a candidate by its index on the current candidate page and commits
    /// it into the buffer. Out-of-range indices and non-composing sessions are ignored.
    /// </summary>
    public void SelectImeCandidate(int index)
    {
        if (_cancelled || _ime is null || !_ime.IsComposing)
        {
            return;
        }

        if (_ime.TrySelectFromCurrentPage(index, out _))
        {
            CommitImeComposition();
        }
    }

    /// <summary>
    /// Changes the number of candidates per page (1–9). The current page resets to
    /// the first page and the selection resets to the first candidate.
    /// </summary>
    public void SetImeCandidatePageSize(int pageSize)
    {
        if (_cancelled || _ime is null)
        {
            return;
        }

        if (_ime.SetPageSize(pageSize))
        {
            _error = null;
            Publish();
        }
    }

    /// <summary>
    /// Moves one candidate page forward or backward. The selection resets to the
    /// first candidate; page movement does not wrap and is ignored without candidates.
    /// </summary>
    public void ChangeImePage(int delta)
    {
        if (_cancelled || _ime is null || !_ime.IsComposing)
        {
            return;
        }

        if (_ime.ChangePage(delta))
        {
            _error = null;
            Publish();
        }
    }

    /// <summary>
    /// Validates the buffer and returns the normalized text. This does not write
    /// to the target control; the host performs the write-back. A pending IME
    /// composition is committed into the buffer first.
    /// </summary>
    public bool TryCommit(out string text, out InputValidationError? error)
    {
        text = _text;
        error = null;

        if (_cancelled)
        {
            error = new InputValidationError(InputValidationErrorCode.CommitRejected);
            SetError(error);
            return false;
        }

        if (!CommitCompositionForAction())
        {
            error = _error;
            text = _text;
            return false;
        }

        if (_effectiveMaxLength > 0 && _text.Length > _effectiveMaxLength)
        {
            error = new InputValidationError(InputValidationErrorCode.MaxLengthExceeded, _effectiveMaxLength);
            SetError(error);
            return false;
        }

        if (!Profile.TryValidate(_text, out var normalized, out var validationError))
        {
            error = validationError ?? new InputValidationError(InputValidationErrorCode.CommitRejected);
            SetError(error);
            return false;
        }

        if (Profile is NumericInputProfile)
        {
            if (!decimal.TryParse(
                    normalized,
                    NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint,
                    CultureInfo.InvariantCulture,
                    out var value))
            {
                error = new InputValidationError(
                    Profile is NumericInputProfile { AllowsDecimalPoint: true }
                        ? InputValidationErrorCode.InvalidDecimal
                        : InputValidationErrorCode.InvalidInteger);
                SetError(error);
                return false;
            }

            if (_minimum.HasValue && value < _minimum.Value)
            {
                error = new InputValidationError(InputValidationErrorCode.ValueTooSmall, _minimum.Value);
                SetError(error);
                return false;
            }

            if (_maximum.HasValue && value > _maximum.Value)
            {
                error = new InputValidationError(InputValidationErrorCode.ValueTooLarge, _maximum.Value);
                SetError(error);
                return false;
            }
        }

        _text = normalized;
        _caret = Math.Min(_caret, _text.Length);
        _error = null;
        text = normalized;
        Publish();
        return true;
    }

    /// <summary>Records a write-back failure reported by the host adapter.</summary>
    public void ReportCommitFailure(InputValidationError error)
    {
        if (error is null)
        {
            throw new ArgumentNullException(nameof(error));
        }

        if (_cancelled)
        {
            return;
        }

        SetError(error);
    }

    /// <summary>Discards the session. No value is written back.</summary>
    public void Cancel()
    {
        if (_cancelled)
        {
            return;
        }

        _cancelled = true;
        _error = null;
        _ime?.Reset();
        Publish();
    }

    private void HandleInsert(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return;
        }

        var text = value!;

        if (_ime is not null)
        {
            if (_ime.IsComposing)
            {
                if (text.Length == 1)
                {
                    var character = text[0];
                    if (IsAsciiLetter(character))
                    {
                        AppendComposition(character);
                        return;
                    }

                    if (character >= '1' && character <= '9')
                    {
                        if (_ime.TrySelectFromCurrentPage(character - '1', out _))
                        {
                            CommitImeComposition();
                        }

                        return;
                    }

                    if (character == '0')
                    {
                        return;
                    }

                    if (character == ' ')
                    {
                        CommitImeComposition();
                        return;
                    }
                }

                if (!CommitCompositionForAction())
                {
                    return;
                }
            }
            else if (_ime.IsEnabled && text.Length == 1 && IsAsciiLetter(text[0]))
            {
                BeginComposition(text[0]);
                return;
            }

            if (_ime.IsEnabled
                && _imeFullWidthPunctuation
                && text.Length == 1
                && _ime.TryMapPunctuation(text[0], out var mapped)
                && !string.IsNullOrEmpty(mapped))
            {
                text = mapped;
            }
        }

        InsertText(text);
    }

    private void InsertText(string text)
    {
        if (TryInsertText(text, reportError: false))
        {
            _error = null;
            Publish();
        }
    }

    /// <summary>
    /// Inserts into the buffer after the maximum-length and intermediate-state
    /// checks. Composition commits use <paramref name="reportError"/> so a failed
    /// candidate insertion keeps the composition and surfaces the error.
    /// </summary>
    private bool TryInsertText(string text, bool reportError)
    {
        var candidate = _text.Insert(_caret, text);
        if (_effectiveMaxLength > 0 && candidate.Length > _effectiveMaxLength)
        {
            if (reportError)
            {
                _error = new InputValidationError(InputValidationErrorCode.MaxLengthExceeded, _effectiveMaxLength);
            }

            return false;
        }

        if (!Profile.IsIntermediateValid(candidate))
        {
            if (reportError)
            {
                _error = new InputValidationError(InputValidationErrorCode.CommitRejected);
            }

            return false;
        }

        _text = candidate;
        _caret += text.Length;
        return true;
    }

    private void Backspace()
    {
        if (_ime is { IsComposing: true })
        {
            if (_ime.RemoveLastLetter())
            {
                _error = null;
                Publish();
            }

            return;
        }

        if (_caret == 0)
        {
            return;
        }

        _text = _text.Remove(_caret - 1, 1);
        _caret--;
        _error = null;
        Publish();
    }

    private void Clear()
    {
        _ime?.Reset();
        _text = string.Empty;
        _caret = 0;
        _error = null;
        Publish();
    }

    private void BeginComposition(char letter)
    {
        if (_ime is null || !_ime.IsEnabled || !IsAsciiLetter(letter))
        {
            return;
        }

        if (_ime.AppendLetter(ToLowerAscii(letter)))
        {
            _error = null;
            Publish();
        }
    }

    private void AppendComposition(char letter)
    {
        if (_ime is null || !IsAsciiLetter(letter))
        {
            return;
        }

        if (_ime.AppendLetter(ToLowerAscii(letter)))
        {
            _error = null;
            Publish();
        }
    }

    private bool HandleImeSelectionMove(int delta)
    {
        if (_ime is null || !_ime.IsComposing)
        {
            return false;
        }

        if (_ime.CandidateCount == 0)
        {
            // Keep the caret in the buffer stable while composing, even when the
            // engine returned no candidate.
            return true;
        }

        if (_ime.MoveSelection(delta))
        {
            _error = null;
            Publish();
        }

        return true;
    }

    private bool CommitCompositionForAction()
        => _ime is not { IsComposing: true } || CommitImeComposition();

    private bool CommitImeComposition()
    {
        if (_ime is null || !_ime.IsComposing)
        {
            return true;
        }

        var text = _ime.CommitText;
        if (string.IsNullOrEmpty(text))
        {
            _ime.Reset();
            _error = null;
            Publish();
            return true;
        }

        if (!TryInsertText(text, reportError: true))
        {
            Publish();
            return false;
        }

        _ime.Reset();
        _error = null;
        Publish();
        return true;
    }

    private void ToggleIme()
    {
        if (_ime is null)
        {
            return;
        }

        if (!CommitCompositionForAction())
        {
            return;
        }

        _ime.ToggleEnabled();
        _error = null;
        _layout = BuildLayout();
        Publish();
    }

    private static bool IsAsciiLetter(char c) => (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z');

    private static char ToLowerAscii(char c) => c >= 'A' && c <= 'Z' ? (char)(c + ('a' - 'A')) : c;

    private void MoveCaret(int delta)
    {
        var next = Clamp(_caret + delta, 0, _text.Length);
        if (next == _caret)
        {
            return;
        }

        _caret = next;
        Publish();
    }

    private void StepDigit(int direction)
    {
        if (!Profile.NumericKeypad.ShowStepKeys || _caret == 0)
        {
            return;
        }

        var index = _caret - 1;
        var current = _text[index];
        if (current < '0' || current > '9')
        {
            return;
        }

        var digit = current - '0';
        var next = (digit + direction + 10) % 10;
        var chars = _text.ToCharArray();
        chars[index] = (char)('0' + next);
        _text = new string(chars);
        _error = null;
        Publish();
    }

    private void ToggleSign()
    {
        if (!Profile.NumericKeypad.ShowSign || !_allowsNegativeSign)
        {
            return;
        }

        string candidate;
        int caret;

        if (_text.Length == 0)
        {
            candidate = "-";
            caret = 1;
        }
        else if (_text[0] == '-')
        {
            candidate = _text.Substring(1);
            caret = Math.Max(0, _caret - 1);
        }
        else
        {
            candidate = "-" + _text;
            caret = Math.Min(candidate.Length, _caret + 1);
        }

        if (!Profile.IsIntermediateValid(candidate))
        {
            return;
        }

        _text = candidate;
        _caret = caret;
        _error = null;
        Publish();
    }

    private void ToggleShift()
    {
        if (_mode != KeyboardMode.FullKeyboard)
        {
            return;
        }

        _shift = !_shift;
        _error = null;
        Publish();
    }

    private void SwitchToFullKeyboard()
    {
        if (_mode == KeyboardMode.FullKeyboard || !Profile.AllowsFullKeyboard)
        {
            return;
        }

        _mode = KeyboardMode.FullKeyboard;
        _page = FullKeyboardPage.Main;
        _shift = false;
        _error = null;
        _layout = BuildLayout();
        Publish();
    }

    private void SwitchToNumericKeypad()
    {
        if (_mode == KeyboardMode.NumericKeypad)
        {
            return;
        }

        _mode = KeyboardMode.NumericKeypad;
        _page = FullKeyboardPage.Main;
        _shift = false;
        _error = null;
        _layout = BuildLayout();
        Publish();
    }

    private void SwitchToSymbolPage()
    {
        if (_mode != KeyboardMode.FullKeyboard
            || _page == FullKeyboardPage.Symbols
            || !KeyboardLayouts.HasSymbolPage(_fullKeyboardLayout))
        {
            return;
        }

        _page = FullKeyboardPage.Symbols;
        _shift = false;
        _error = null;
        _layout = BuildLayout();
        Publish();
    }

    private void SwitchToMainPage()
    {
        if (_mode != KeyboardMode.FullKeyboard || _page == FullKeyboardPage.Main)
        {
            return;
        }

        _page = FullKeyboardPage.Main;
        _shift = false;
        _error = null;
        _layout = BuildLayout();
        Publish();
    }

    private KeyboardLayout BuildLayout()
    {
        if (_mode == KeyboardMode.FullKeyboard)
        {
            return KeyboardLayouts.Full(
                _fullKeyboardLayout,
                _page,
                BuildImeKey(),
                BuildSymbolLabel());
        }

        return Profile.AllowsFullKeyboard
            ? KeyboardLayouts.GeneralNumeric(_numericKeypadLayout)
            : KeyboardLayouts.ProfileNumeric(Profile);
    }

    /// <summary>
    /// The symbol page shows the active language's punctuation where the engine
    /// provides a mapping; the command still inserts the ASCII source so the
    /// session's punctuation routing stays the single source of truth.
    /// </summary>
    private Func<char, string?>? BuildSymbolLabel()
    {
        if (_page != FullKeyboardPage.Symbols || _ime is null || !_imeFullWidthPunctuation)
        {
            return null;
        }

        return c => _ime.TryMapPunctuation(c, out var mapped) && !string.IsNullOrEmpty(mapped)
            ? mapped
            : null;
    }

    /// <summary>
    /// Builds the IME toggle for the full keyboard. The key stays present in
    /// English mode (so switching back is always possible); only the label and
    /// accessible name follow the active state.
    /// </summary>
    private KeyDefinition? BuildImeKey()
    {
        if (_ime is null)
        {
            return null;
        }

        var active = _ime.IsEnabled;
        var width = _fullKeyboardLayout == FullKeyboardLayoutKind.AnsiStyle ? 1.5 : 1.0;
        return new KeyDefinition(
            id: "ime.toggle",
            label: KeyLabel.Resource(active ? "key.ime.active" : "key.ime.inactive"),
            command: KeyCommand.ToggleIme,
            widthUnits: width,
            style: KeyStyle.Action,
            repeatable: false,
            shiftedLabel: null,
            shiftedCommand: null,
            columnSpan: 1,
            rowSpan: 1,
            glyph: null,
            accessibleName: KeyLabel.Resource(active ? "key.ime.active.name" : "key.ime.inactive.name"));
    }

    private void SetError(InputValidationError error)
    {
        _error = error;
        Publish();
    }

    private void Publish()
    {
        State = Snapshot();
        StateChanged?.Invoke(this, new KeyboardStateChangedEventArgs(State));
    }

    private KeyboardState Snapshot()
        => new KeyboardState(
            _text,
            _caret,
            _mode,
            _shift,
            _layout,
            _fullKeyboardLayout,
            _page,
            _numericKeypadLayout,
            _error,
            !string.Equals(_text, _initialText, StringComparison.Ordinal),
            _cancelled,
            _ime?.Snapshot());

    private static int ResolveMaxLength(int? profileMaxLength, int targetMaxLength)
    {
        if (profileMaxLength is { } profile && profile > 0)
        {
            return targetMaxLength > 0 ? Math.Min(profile, targetMaxLength) : profile;
        }

        return targetMaxLength > 0 ? targetMaxLength : 0;
    }

    private static int Clamp(int value, int min, int max)
        => value < min ? min : value > max ? max : value;
}
