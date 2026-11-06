namespace TouchKeyboard.Core;

/// <summary>
/// A named input contract for one field: validation, intermediate-state rules,
/// default mode, numeric key set and commit normalization.
/// </summary>
public abstract class InputProfile
{
    /// <summary>Stable profile id, for example <c>positive-decimal</c>.</summary>
    public abstract string Id { get; }

    /// <summary>The mode the keyboard opens in for this profile.</summary>
    public virtual KeyboardMode DefaultMode => KeyboardMode.FullKeyboard;

    /// <summary>Whether the full keyboard is allowed; numeric profiles return false.</summary>
    public virtual bool AllowsFullKeyboard => true;

    /// <summary>Keys exposed by the numeric keypad for this profile.</summary>
    public virtual NumericKeypadOptions NumericKeypad => NumericKeypadOptions.General;

    /// <summary>An optional profile-level maximum length; the session combines it with the target's limit.</summary>
    public virtual int? MaxLength => null;

    /// <summary>Whether an empty value may be committed.</summary>
    public virtual bool AllowEmpty => true;

    /// <summary>
    /// Whether the text is a legal editing state. Intermediate states such as
    /// "-", "1." or "2026/" are normally legal; commit validation is separate.
    /// </summary>
    public virtual bool IsIntermediateValid(string text) => true;

    /// <summary>
    /// Validates and normalizes a committed value.
    /// </summary>
    public abstract bool TryValidate(
        string text,
        out string normalized,
        out InputValidationError? error);
}
