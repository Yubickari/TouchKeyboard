using TouchKeyboard.Core;
using Xunit;

namespace TouchKeyboard.Core.Tests;

public sealed class KeyboardModeTests
{
    [Fact]
    public void KeyboardMode_values_are_stable()
    {
        Assert.Equal(0, (int)KeyboardMode.FullKeyboard);
        Assert.Equal(1, (int)KeyboardMode.NumericKeypad);
    }

    [Fact]
    public void Full_keyboard_layout_kinds_are_distinct()
    {
        Assert.Equal(0, (int)FullKeyboardLayoutKind.PhoneHybrid);
        Assert.Equal(1, (int)FullKeyboardLayoutKind.AnsiStrict);
        Assert.NotEqual(FullKeyboardLayoutKind.PhoneHybrid, FullKeyboardLayoutKind.AnsiStrict);
    }

    [Fact]
    public void Numeric_keypad_layout_kinds_are_distinct()
    {
        Assert.Equal(0, (int)NumericKeypadLayoutKind.Numpad);
        Assert.Equal(1, (int)NumericKeypadLayoutKind.Phone);
        Assert.NotEqual(NumericKeypadLayoutKind.Numpad, NumericKeypadLayoutKind.Phone);
    }
}
