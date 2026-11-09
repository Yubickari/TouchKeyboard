using System;
using Avalonia.Media;

namespace TouchKeyboard.Avalonia;

/// <summary>
/// Platform-aware default font families for the overlay. The first family in a
/// chain is the preferred UI face; the following families cover a missing face or
/// CJK glyphs. Hosts can replace the effective family through
/// <see cref="TouchKeyboardStyle.FontFamily"/> or a full
/// <see cref="TouchKeyboardTheme"/>.
/// </summary>
/// <remarks>
/// Avalonia resolves a comma-separated <see cref="FontFamily"/> as a fallback
/// chain. The names are chosen from fonts that ship with the desktop platform;
/// Linux images still need a CJK face (for example Noto Sans CJK SC) for Chinese
/// text, exactly like the rest of the host application.
/// </remarks>
public static class TouchKeyboardFonts
{
    /// <summary>Windows UI stack: Segoe UI with a Microsoft YaHei CJK fallback.</summary>
    public static FontFamily Windows { get; } = new FontFamily(
        "Segoe UI, Microsoft YaHei UI, Microsoft YaHei");

    /// <summary>macOS UI stack: Helvetica Neue with a PingFang CJK fallback.</summary>
    public static FontFamily MacOS { get; } = new FontFamily(
        "Helvetica Neue, PingFang SC, Hiragino Sans GB");

    /// <summary>Linux UI stack: Noto Sans with common Noto CJK and DejaVu fallbacks.</summary>
    public static FontFamily Linux { get; } = new FontFamily(
        "Noto Sans, Noto Sans CJK SC, WenQuanYi Micro Hei, DejaVu Sans");

    /// <summary>
    /// The default for the current operating system. An unknown platform uses
    /// Avalonia's own default family.
    /// </summary>
    public static FontFamily Default { get; } = ResolveDefault();

    private static FontFamily ResolveDefault()
    {
        if (OperatingSystem.IsWindows())
        {
            return Windows;
        }

        if (OperatingSystem.IsMacOS())
        {
            return MacOS;
        }

        if (OperatingSystem.IsLinux())
        {
            return Linux;
        }

        return FontFamily.Default;
    }
}
