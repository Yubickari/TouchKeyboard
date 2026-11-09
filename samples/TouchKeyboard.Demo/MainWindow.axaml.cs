using System;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.VisualTree;
using TouchKeyboard.Avalonia;
using TouchKeyboard.Core;
using TouchKeyboard.Ime.Pinyin;

namespace TouchKeyboard.Demo;

public partial class MainWindow : Window
{
    private readonly PinyinImeEngine _engine = new PinyinImeEngine();
    private bool _updatingAppearance;

    public MainWindow()
    {
        InitializeComponent();
        KeyboardHost.Configure(options => options.UseIme(_engine));
        Opened += (_, _) => ApplyDemoTheme();
        ActualThemeVariantChanged += (_, _) => ApplyDemoTheme();
        if (AppearancePanel is not null)
        {
            AppearancePanel.Expanded += (_, _) => ApplyDemoTheme();
        }

        ApplyAppearance();
        ApplyDemoTheme();
    }

    private void OnLayoutChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (!_updatingAppearance)
        {
            ApplyAppearance();
        }
    }

    private void OnAppearanceChanged(object? sender, RangeBaseValueChangedEventArgs e)
    {
        if (!_updatingAppearance)
        {
            ApplyAppearance();
        }
    }

    private void OnColorChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (!_updatingAppearance)
        {
            ApplyAppearance();
        }
    }

    private void OnLanguageOrFontChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (!_updatingAppearance)
        {
            ApplyAppearance();
        }
    }

    private void OnDragChanged(object? sender, RoutedEventArgs e)
    {
        if (!_updatingAppearance)
        {
            ApplyAppearance();
        }
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

    private void OnResetPosition(object? sender, RoutedEventArgs e)
        => KeyboardHost?.ResetPanelPosition();

    private void OnResetAppearance(object? sender, RoutedEventArgs e)
    {
        _updatingAppearance = true;
        try
        {
            if (ScaleSlider is not null)
            {
                ScaleSlider.Value = 1;
            }

            if (FontScaleSlider is not null)
            {
                FontScaleSlider.Value = 1;
            }

            if (IconScaleSlider is not null)
            {
                IconScaleSlider.Value = 1;
            }

            if (AccentSelector is not null)
            {
                AccentSelector.SelectedIndex = 0;
            }

            if (ConfirmSelector is not null)
            {
                ConfirmSelector.SelectedIndex = 0;
            }

            if (CandidateSelector is not null)
            {
                CandidateSelector.SelectedIndex = 0;
            }

            if (PanelSelector is not null)
            {
                PanelSelector.SelectedIndex = 0;
            }

            if (KeySelector is not null)
            {
                KeySelector.SelectedIndex = 0;
            }

            if (DragCheckBox is not null)
            {
                DragCheckBox.IsChecked = true;
            }

            if (LayoutSelector is not null)
            {
                LayoutSelector.SelectedIndex = 0;
            }

            if (ThemeSelector is not null)
            {
                ThemeSelector.SelectedIndex = 0;
            }

            if (LanguageSelector is not null)
            {
                LanguageSelector.SelectedIndex = 0;
            }

            if (FontSelector is not null)
            {
                FontSelector.SelectedIndex = 0;
            }
        }
        finally
        {
            _updatingAppearance = false;
        }

        if (Application.Current is { } application)
        {
            application.RequestedThemeVariant = ThemeVariant.Default;
        }

        ApplyAppearance();
        KeyboardHost?.ResetPanelPosition();
    }

    private void ApplyDemoTheme()
    {
        var isDark = ActualThemeVariant == ThemeVariant.Dark;
        var text = new SolidColorBrush(Color.Parse(isDark ? "#FFF9FAFB" : "#FF1F2937"));
        var muted = new SolidColorBrush(Color.Parse(isDark ? "#FF9CA3AF" : "#FF4B5563"));

        if (AppearancePanel is not null)
        {
            foreach (var textBlock in AppearancePanel.GetVisualDescendants().OfType<TextBlock>())
            {
                textBlock.Foreground = text;
            }
        }

        if (TestInstructionsText is not null)
        {
            TestInstructionsText.Foreground = muted;
        }
    }

    private void ApplyAppearance()
    {
        if (KeyboardHost is null)
        {
            return;
        }

        var layout = LayoutSelector?.SelectedItem is ComboBoxItem { Tag: string layoutTag }
                     && Enum.TryParse<FullKeyboardLayoutKind>(layoutTag, out var parsedLayout)
            ? parsedLayout
            : FullKeyboardLayoutKind.AnsiStyle;

        var language = LanguageSelector?.SelectedItem is ComboBoxItem { Tag: string languageTag }
                       && Enum.TryParse<KeyboardLanguage>(languageTag, out var parsedLanguage)
            ? parsedLanguage
            : KeyboardLanguage.Auto;

        KeyboardHost.Configure(options =>
        {
            options.FullKeyboardLayout = layout;
            options.KeyLanguage = language;
            options.Style.Scale = ScaleSlider?.Value ?? 1.0;
            options.Style.FontScale = FontScaleSlider?.Value ?? 1.0;
            options.Style.IconScale = IconScaleSlider?.Value ?? 1.0;
            options.Style.FontFamily = FontFamilyFor(SelectedTag(FontSelector));
            options.Style.AccentColor = AccentColorFor(SelectedTag(AccentSelector));
            options.Style.ConfirmColor = ConfirmColorFor(SelectedTag(ConfirmSelector));
            options.Style.SelectedCandidateColor = CandidateColorFor(SelectedTag(CandidateSelector));
            options.Style.PanelColor = PanelColorFor(SelectedTag(PanelSelector));
            options.Style.KeyColor = KeyColorFor(SelectedTag(KeySelector));
            options.EnablePanelDrag = DragCheckBox?.IsChecked ?? true;
        });

        UpdateValueTexts();
    }

    private void UpdateValueTexts()
    {
        if (ScaleValueText is not null && ScaleSlider is not null)
        {
            ScaleValueText.Text = ScaleSlider.Value.ToString("0.00", CultureInfo.CurrentCulture);
        }

        if (FontScaleValueText is not null && FontScaleSlider is not null)
        {
            FontScaleValueText.Text = FontScaleSlider.Value.ToString("0.00", CultureInfo.CurrentCulture);
        }

        if (IconScaleValueText is not null && IconScaleSlider is not null)
        {
            IconScaleValueText.Text = IconScaleSlider.Value.ToString("0.00", CultureInfo.CurrentCulture);
        }
    }

    private static string? SelectedTag(ComboBox? selector)
        => (selector?.SelectedItem as ComboBoxItem)?.Tag as string;

    private static Color? AccentColorFor(string? tag) => tag switch
    {
        "Teal" => Color.Parse("#FF0F766E"),
        "Blue" => Color.Parse("#FF2563EB"),
        "Orange" => Color.Parse("#FFEA580C"),
        "Purple" => Color.Parse("#FF7C3AED"),
        _ => null
    };

    private static Color? ConfirmColorFor(string? tag) => tag switch
    {
        "Green" => Color.Parse("#FF15803D"),
        "Orange" => Color.Parse("#FFC2410C"),
        "Red" => Color.Parse("#FFDC2626"),
        _ => null
    };

    private static Color? CandidateColorFor(string? tag) => tag switch
    {
        "Mint" => Color.Parse("#FFCCFBF1"),
        "Amber" => Color.Parse("#FFFEF3C7"),
        "Lavender" => Color.Parse("#FFEDE9FE"),
        _ => null
    };

    private static Color? PanelColorFor(string? tag) => tag switch
    {
        "Light" => Color.Parse("#FFF1F5F9"),
        "White" => Colors.White,
        _ => null
    };

    private static Color? KeyColorFor(string? tag) => tag switch
    {
        "White" => Colors.White,
        "Gray" => Color.Parse("#FFF1F5F9"),
        _ => null
    };

    private static FontFamily? FontFamilyFor(string? tag) => tag switch
    {
        "Cjk" => new FontFamily("Microsoft YaHei UI, PingFang SC, Noto Sans CJK SC, Noto Sans"),
        "Mono" => new FontFamily("Consolas, Menlo, DejaVu Sans Mono, Noto Sans Mono"),
        "Serif" => new FontFamily("Georgia, Times New Roman, Songti SC, Noto Serif CJK SC"),
        _ => null
    };
}
