using System;
using System.Globalization;

namespace TouchKeyboard.Core;

/// <summary>
/// Owns the edit buffer, caret, mode, shift state and commit/cancel lifecycle for
/// one field. Closing always discards; only <see cref="TryCommit"/> produces a
/// value for the host to write back.
/// </summary>
public sealed class KeyboardSession
{
    private readonly string _initialText;
    private readonly int _effectiveMaxLength;
    private readonly bool _allowsNegativeSign;
    private readonly decimal? _minimum;
    private readonly decimal? _maximum;
    private readonly FullKeyboardLayoutKind _fullKeyboardLayout;
    private NumericKeypadLayoutKind _numericKeypadLayout;

    private string _text;
    private int _caret;
    private KeyboardMode _mode;
    private bool _shift;
    private InputValidationError? _error;
    private bool _cancelled;
    private KeyboardLayout _layout;

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
        _effectiveMaxLength = ResolveMaxLength(Profile.MaxLength, options.MaxLength);

        _text = options.Text ?? string.Empty;
        _initialText = _text;
        _caret = _text.Length;
        _mode = Profile.DefaultMode;
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
                Insert(command.Text);
                return;
            case KeyCommandKind.Backspace:
                Backspace();
                return;
            case KeyCommandKind.Clear:
                Clear();
                return;
            case KeyCommandKind.MoveCaretLeft:
                MoveCaret(-1);
                return;
            case KeyCommandKind.MoveCaretRight:
                MoveCaret(1);
                return;
            case KeyCommandKind.StepDigitUp:
                StepDigit(1);
                return;
            case KeyCommandKind.StepDigitDown:
                StepDigit(-1);
                return;
            case KeyCommandKind.ToggleSign:
                ToggleSign();
                return;
            case KeyCommandKind.ToggleShift:
                ToggleShift();
                return;
            case KeyCommandKind.SwitchToFullKeyboard:
                SwitchToFullKeyboard();
                return;
            case KeyCommandKind.SwitchToNumericKeypad:
                SwitchToNumericKeypad();
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
    /// Validates the buffer and returns the normalized text. This does not write
    /// to the target control; the host performs the write-back.
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
        Publish();
    }

    private void Insert(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return;
        }

        var text = value!;
        var candidate = _text.Insert(_caret, text);
        if (_effectiveMaxLength > 0 && candidate.Length > _effectiveMaxLength)
        {
            return;
        }

        if (!Profile.IsIntermediateValid(candidate))
        {
            return;
        }

        _text = candidate;
        _caret += text.Length;
        _error = null;
        Publish();
    }

    private void Backspace()
    {
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
        _text = string.Empty;
        _caret = 0;
        _error = null;
        Publish();
    }

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
        _shift = false;
        _error = null;
        _layout = BuildLayout();
        Publish();
    }

    private KeyboardLayout BuildLayout()
    {
        if (_mode == KeyboardMode.FullKeyboard)
        {
            return KeyboardLayouts.Full(_fullKeyboardLayout);
        }

        return Profile.AllowsFullKeyboard
            ? KeyboardLayouts.GeneralNumeric(_numericKeypadLayout)
            : KeyboardLayouts.ProfileNumeric(Profile);
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
            _numericKeypadLayout,
            _error,
            !string.Equals(_text, _initialText, StringComparison.Ordinal),
            _cancelled);

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
