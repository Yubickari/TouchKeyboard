namespace TouchKeyboard.Core;

/// <summary>
/// Visual role of a key. The frontend maps these roles to its theme.
/// </summary>
public enum KeyStyle
{
    /// <summary>Ordinary character key.</summary>
    Default = 0,

    /// <summary>Function key such as Shift, Backspace or an arrow.</summary>
    Action = 1,

    /// <summary>Primary action, normally Confirm.</summary>
    Primary = 2,

    /// <summary>Destructive action such as Close.</summary>
    Danger = 3,

    /// <summary>An empty cell that keeps the numeric grid aligned.</summary>
    Spacer = 4
}
