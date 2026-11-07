using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Styling;
using TouchKeyboard.Core;

namespace TouchKeyboard.Demo;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void OnLayoutChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (KeyboardHost is null
            || LayoutSelector?.SelectedItem is not ComboBoxItem { Tag: string tag }
            || !Enum.TryParse<FullKeyboardLayoutKind>(tag, out var kind))
        {
            return;
        }

        KeyboardHost.Configure(options => options.FullKeyboardLayout = kind);
    }

    private void OnThemeChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (ThemeSelector?.SelectedItem is not ComboBoxItem { Tag: string tag }
            || Application.Current is not { } application)
        {
            return;
        }

        application.RequestedThemeVariant = tag switch
        {
            "Light" => ThemeVariant.Light,
            "Dark" => ThemeVariant.Dark,
            _ => ThemeVariant.Default
        };
    }
}
