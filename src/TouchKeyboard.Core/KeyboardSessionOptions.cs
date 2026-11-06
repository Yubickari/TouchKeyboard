namespace TouchKeyboard.Core;

/// <summary>
/// Inputs for one keyboard session: the initial value, profile, field metadata
/// and the target control's limits.
/// </summary>
public sealed class KeyboardSessionOptions
{
    /// <summary>The value captured from the target control when the panel opens.</summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>The input contract for the field.</summary>
    public InputProfile Profile { get; set; } = InputProfiles.Text;

    /// <summary>Field description shown in the title row.</summary>
    public string? Label { get; set; }

    /// <summary>Unit shown next to the description, for example "℃".</summary>
    public string? Unit { get; set; }

    /// <summary>Target maximum length; 0 means unlimited. Combined with the profile limit.</summary>
    public int MaxLength { get; set; }

    /// <summary>Whether the target accepts a negative value. Positive profiles hide the sign key anyway.</summary>
    public bool AllowsNegativeSign { get; set; } = true;

    /// <summary>Optional minimum for numeric profiles.</summary>
    public decimal? Minimum { get; set; }

    /// <summary>Optional maximum for numeric profiles.</summary>
    public decimal? Maximum { get; set; }

    /// <summary>Full-keyboard geometry; AnsiStrict is reserved for a future release.</summary>
    public FullKeyboardLayoutKind FullKeyboardLayout { get; set; } = FullKeyboardLayoutKind.PhoneHybrid;

    /// <summary>General numeric keypad surface.</summary>
    public NumericKeypadLayoutKind NumericKeypadLayout { get; set; } = NumericKeypadLayoutKind.Numpad;
}
