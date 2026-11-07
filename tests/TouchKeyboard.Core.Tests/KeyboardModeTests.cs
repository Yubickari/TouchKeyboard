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
        Assert.Equal(1, (int)FullKeyboardLayoutKind.AnsiStyle);
        Assert.NotEqual(FullKeyboardLayoutKind.PhoneHybrid, FullKeyboardLayoutKind.AnsiStyle);
    }

    [Fact]
    public void Numeric_keypad_layout_kinds_are_distinct()
    {
        Assert.Equal(0, (int)NumericKeypadLayoutKind.Numpad);
        Assert.Equal(1, (int)NumericKeypadLayoutKind.Sequence);
        Assert.NotEqual(NumericKeypadLayoutKind.Numpad, NumericKeypadLayoutKind.Sequence);
    }

    [Fact]
    public void Options_default_to_the_ansi_style_full_keyboard_and_the_numpad()
    {
        var options = new KeyboardSessionOptions();

        Assert.Equal(FullKeyboardLayoutKind.AnsiStyle, options.FullKeyboardLayout);
        Assert.Equal(NumericKeypadLayoutKind.Numpad, options.NumericKeypadLayout);
    }

    [Fact]
    public void Full_keyboard_pages_are_stable()
    {
        Assert.Equal(0, (int)FullKeyboardPage.Main);
        Assert.Equal(1, (int)FullKeyboardPage.Symbols);
    }
}
