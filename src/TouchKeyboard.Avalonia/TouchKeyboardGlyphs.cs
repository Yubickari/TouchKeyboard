using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Avalonia.Media;
using TouchKeyboard.Core;
using AvaloniaPath = Avalonia.Controls.Shapes.Path;

namespace TouchKeyboard.Avalonia;

/// <summary>
/// Vector renderer for the semantic <see cref="KeyGlyph"/> icons. Every glyph is
/// drawn on one shared 24×24 design canvas, so features keep their relative
/// position between glyphs: the space bar lines up with the eraser baseline, and
/// the password eye does not move when only its slash appears. Each glyph carries
/// a size factor that scales its canvas to the intended optical size.
/// </summary>
internal static class TouchKeyboardGlyphs
{
    /// <summary>Side of the shared design canvas, in design units.</summary>
    public const double DesignSize = 24;

    private static readonly Dictionary<KeyGlyph, GlyphShape> Shapes = CreateShapes();

    /// <summary>Looks up one glyph.</summary>
    public static bool TryGet(KeyGlyph glyph, [NotNullWhen(true)] out GlyphShape? shape)
        => Shapes.TryGetValue(glyph, out shape);

    /// <summary>Applies the glyph geometry and paint to a path drawn on the design canvas.</summary>
    public static bool Apply(AvaloniaPath path, KeyGlyph glyph, IBrush brush)
    {
        if (!Shapes.TryGetValue(glyph, out var shape))
        {
            path.IsVisible = false;
            return false;
        }

        path.Data = shape.Geometry;
        path.Fill = shape.Filled ? brush : null;
        path.Stroke = shape.Filled ? null : brush;
        path.StrokeThickness = shape.DesignedStroke;
        path.Width = DesignSize;
        path.Height = DesignSize;
        path.Stretch = Stretch.None;
        path.IsVisible = true;
        return true;
    }

    private static Dictionary<KeyGlyph, GlyphShape> CreateShapes()
        => new Dictionary<KeyGlyph, GlyphShape>
        {
            // Tag with an X: delete to the left.
            [KeyGlyph.Backspace] = Stroke(
                "M 9 5 H 19 A 2 2 0 0 1 21 7 V 17 A 2 2 0 0 1 19 19 H 9 L 3 12 Z M 11 9 L 17 15 M 17 9 L 11 15",
                sizeFactor: 24d / 18d),

            // Eraser with a baseline at y=21.5: clear everything, distinct from Backspace.
            [KeyGlyph.Clear] = Stroke(
                "M 14.5 2.5 L 20.5 8.5 L 10 19 L 4 13 Z M 9.25 7.75 L 15.25 13.75 M 4 21.5 H 21",
                sizeFactor: 24d / 19d),

            // Return arrow.
            [KeyGlyph.Enter] = Stroke(
                "M 20 4 V 11 A 3 3 0 0 1 17 14 H 6 M 10.5 9.5 L 6 14 L 10.5 18.5",
                sizeFactor: 24d / 14.5),

            // Compact shift chevron, and its pressed-in filled state.
            [KeyGlyph.Shift] = Stroke(
                "M 12 3 L 21 12 H 16.5 V 21 H 7.5 V 12 H 3 Z",
                sizeFactor: 0.78 * 24d / 18d),
            [KeyGlyph.ShiftFilled] = Filled(
                "M 12 3 L 21 12 H 16.5 V 21 H 7.5 V 12 H 3 Z",
                sizeFactor: 0.78 * 24d / 18d),

            // Caret arrows: a filled triangle plus a caret bar reads as cursor movement.
            [KeyGlyph.ArrowLeft] = Filled(
                "M 5 12 L 15 6 V 18 Z M 17 6 H 19 V 18 H 17 Z",
                sizeFactor: 0.86 * 24d / 14d),
            [KeyGlyph.ArrowRight] = Filled(
                "M 19 12 L 9 6 V 18 Z M 5 6 H 7 V 18 H 5 Z",
                sizeFactor: 0.86 * 24d / 14d),

            // Step arrows: an outlined triangle with a + or - sign for increase/decrease.
            [KeyGlyph.ArrowUp] = Stroke(
                "M 12 2 L 18 8 H 6 Z M 12 11.5 V 19.5 M 8 15.5 H 16",
                sizeFactor: 0.86 * 24d / 17.5),
            [KeyGlyph.ArrowDown] = Stroke(
                "M 8 4.5 H 16 M 12 22 L 6 16 H 18 Z",
                sizeFactor: 0.86 * 24d / 17.5),

            // Space bar: a wide U whose bottom line sits on the same y as the eraser
            // baseline (21.5), so adjacent keys line up.
            [KeyGlyph.Space] = Stroke(
                "M 2 14 V 20 A 1.5 1.5 0 0 0 3.5 21.5 H 20.5 A 1.5 1.5 0 0 0 22 20 V 14",
                sizeFactor: 24d / 19d),

            // Password visibility: an eye and the same eye with a slash. Both use the
            // same canvas and factor so toggling never moves the eye itself.
            [KeyGlyph.ShowPassword] = Stroke(
                "M 2.5 12 C 5.5 7.2 9 4.5 12 4.5 C 15 4.5 18.5 7.2 21.5 12 C 18.5 16.8 15 19.5 12 19.5 C 9 19.5 5.5 16.8 2.5 12 Z M 12 8.5 A 3.5 3.5 0 1 1 12 15.5 A 3.5 3.5 0 1 1 12 8.5 Z",
                sizeFactor: 24d / 19d),
            [KeyGlyph.HidePassword] = Stroke(
                "M 2.5 12 C 5.5 7.2 9 4.5 12 4.5 C 15 4.5 18.5 7.2 21.5 12 C 18.5 16.8 15 19.5 12 19.5 C 9 19.5 5.5 16.8 2.5 12 Z M 12 8.5 A 3.5 3.5 0 1 1 12 15.5 A 3.5 3.5 0 1 1 12 8.5 Z M 3.5 3.5 L 20.5 20.5",
                sizeFactor: 24d / 19d),

            // Sign toggle: a plus over a minus, drawn larger than the action font so
            // it reads at the same optical size as the digit keys.
            [KeyGlyph.PlusMinus] = Stroke(
                "M 12 6 V 13 M 8.5 9.5 H 15.5 M 8.5 18 H 15.5",
                sizeFactor: 1.6)
        };

    private static GlyphShape Stroke(string data, double sizeFactor)
        => new(Geometry.Parse(data), filled: false, sizeFactor);

    private static GlyphShape Filled(string data, double sizeFactor)
        => new(Geometry.Parse(data), filled: true, sizeFactor);

    /// <summary>One glyph's geometry, paint mode and canvas scale.</summary>
    internal sealed class GlyphShape
    {
        public GlyphShape(Geometry geometry, bool filled, double sizeFactor)
        {
            Geometry = geometry;
            Filled = filled;
            SizeFactor = sizeFactor;

            // Keep the rendered stroke weight equal across glyphs regardless of their
            // canvas scale: the reference is 0.09 of the optical icon size.
            DesignedStroke = sizeFactor <= 0 ? 1.6 : 24 * 0.09 / sizeFactor;
        }

        public Geometry Geometry { get; }

        public bool Filled { get; }

        /// <summary>Canvas scale relative to the optical icon size.</summary>
        public double SizeFactor { get; }

        /// <summary>Stroke thickness in design units.</summary>
        public double DesignedStroke { get; }
    }
}
