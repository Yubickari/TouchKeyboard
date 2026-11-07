namespace TouchKeyboard.Core;

/// <summary>
/// Which semantic key fills a Numpad row-2 side slot. A slot left as
/// <see cref="None"/> stays empty, and the neighbouring key grows into it: 0
/// widens over an empty decimal slot and Confirm stretches over an empty
/// right-row2 slot.
/// </summary>
public enum NumpadSlotKey
{
    /// <summary>No key; the slot stays empty.</summary>
    None = 0,

    /// <summary>The ABC key that returns to the full keyboard.</summary>
    FullKeyboard = 1,

    /// <summary>The ± sign toggle.</summary>
    Sign = 2
}
