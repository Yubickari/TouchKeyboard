using System;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using Avalonia.Threading;
using TouchKeyboard.Core;

namespace TouchKeyboard.Avalonia;

/// <summary>
/// Default single-line editor surface. It draws the buffer, caret and password
/// mask itself, so it is not a TextBox and does not trigger the system on-screen
/// keyboard or IME.
/// </summary>
public sealed class TouchKeyboardEditor : Control, IEditorSurface
{
    private readonly DispatcherTimer _caretTimer;
    private KeyboardSession? _session;
    private bool _caretVisible = true;
    private bool _revealPassword;
    private double _scrollOffset;

    /// <summary>Creates the editor surface.</summary>
    public TouchKeyboardEditor()
    {
        Focusable = false;
        ClipToBounds = true;

        _caretTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(530) };
        _caretTimer.Tick += (_, _) =>
        {
            _caretVisible = !_caretVisible;
            InvalidateVisual();
        };
    }

    /// <inheritdoc />
    public Control View => this;

    /// <summary>The current display text.</summary>
    public string Text { get; private set; } = string.Empty;

    /// <summary>The caret position in the real text.</summary>
    public int CaretIndex { get; private set; }

    /// <summary>
    /// Horizontal offset of the caret in the drawn text, in logical pixels. Trailing
    /// spaces are measured, so the caret visibly follows them.
    /// </summary>
    public double CaretOffset => MeasureCaretX(GetDisplayText());

    /// <summary>Whether the text is masked.</summary>
    public bool IsPassword { get; private set; }

    /// <summary>Whether a password is temporarily revealed.</summary>
    public bool RevealPassword
    {
        get => _revealPassword;
        set
        {
            if (_revealPassword == value)
            {
                return;
            }

            _revealPassword = value;
            _caretVisible = true;
            InvalidateVisual();
        }
    }

    /// <summary>Placeholder shown when the buffer is empty.</summary>
    public string PlaceholderText { get; set; } = string.Empty;

    /// <summary>Text brush for the value.</summary>
    public IBrush TextBrush { get; set; } = Brushes.Black;

    /// <summary>Muted brush for the placeholder.</summary>
    public IBrush MutedBrush { get; set; } = Brushes.Gray;

    /// <summary>Caret brush.</summary>
    public IBrush CaretBrush { get; set; } = Brushes.Blue;

    /// <summary>Editor font family.</summary>
    public FontFamily EditorFontFamily { get; set; } = FontFamily.Default;

    /// <summary>Editor font size.</summary>
    public double EditorFontSize { get; set; } = 28;

    /// <summary>Horizontal padding.</summary>
    public Thickness EditorPadding { get; set; } = new Thickness(14, 0);

    /// <summary>Mask character.</summary>
    public char PasswordChar { get; set; } = '•';

    /// <inheritdoc />
    public void Attach(KeyboardSession session)
    {
        if (session is null)
        {
            throw new ArgumentNullException(nameof(session));
        }

        Detach();
        _session = session;
        session.StateChanged += OnSessionChanged;
        Apply(session.State);
        _caretTimer.Start();
    }

    /// <inheritdoc />
    public void Detach()
    {
        if (_session is not null)
        {
            _session.StateChanged -= OnSessionChanged;
            _session = null;
        }

        _caretTimer.Stop();
    }

    /// <inheritdoc />
    protected override Size MeasureOverride(Size availableSize)
        => new Size(0, double.IsInfinity(availableSize.Height) ? EditorFontSize * 1.5 : availableSize.Height);

    /// <inheritdoc />
    public override void Render(DrawingContext context)
    {
        base.Render(context);

        using (context.PushClip(new Rect(Bounds.Size)))
        {
            var display = GetDisplayText();
            var text = string.IsNullOrEmpty(display)
                ? Format(PlaceholderText, MutedBrush)
                : Format(display, TextBrush);

            var viewportWidth = Math.Max(1, Bounds.Width - EditorPadding.Left - EditorPadding.Right);
            var textWidth = MeasureTextWidth(display);
            var caretX = MeasureCaretX(display);

            if (caretX + EditorPadding.Left - _scrollOffset > Bounds.Width - EditorPadding.Right)
            {
                _scrollOffset = caretX + EditorPadding.Left - (Bounds.Width - EditorPadding.Right);
            }
            else if (caretX + EditorPadding.Left - _scrollOffset < EditorPadding.Left)
            {
                _scrollOffset = Math.Max(0, caretX);
            }

            var maxScroll = Math.Max(0, textWidth - viewportWidth);
            _scrollOffset = Math.Max(0, Math.Min(_scrollOffset, maxScroll));

            var textY = Math.Max(0, (Bounds.Height - text.Height) / 2);
            context.DrawText(text, new Point(EditorPadding.Left - _scrollOffset, textY));

            if (_caretVisible)
            {
                var caretHeight = Math.Max(EditorFontSize, text.Height);
                var caretY = Math.Max(0, (Bounds.Height - caretHeight) / 2);
                var caret = new Rect(
                    EditorPadding.Left - _scrollOffset + caretX,
                    caretY,
                    2,
                    caretHeight);
                context.DrawRectangle(CaretBrush, null, caret);
            }
        }
    }

    private void OnSessionChanged(object? sender, KeyboardStateChangedEventArgs e) => Apply(e.State);

    private void Apply(KeyboardState state)
    {
        Text = state.Text;
        CaretIndex = state.CaretIndex;
        IsPassword = _session?.Profile.Id == "password";
        _caretVisible = true;
        InvalidateVisual();
    }

    private string GetDisplayText()
    {
        if (!IsPassword || RevealPassword)
        {
            return Text;
        }

        return new string(PasswordChar, Text.Length);
    }

    private double MeasureCaretX(string display)
    {
        if (CaretIndex <= 0)
        {
            return 0;
        }

        var count = Math.Min(CaretIndex, display.Length);
        return MeasureTextWidth(display.Substring(0, count));
    }

    /// <summary>
    /// Measures text including trailing whitespace. Skia drops trailing spaces from
    /// plain text widths, which would freeze the caret after a typed space.
    /// </summary>
    private double MeasureTextWidth(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return 0;
        }

        var layout = new TextLayout(
            text,
            new Typeface(EditorFontFamily, FontStyle.Normal, FontWeight.Normal, FontStretch.Normal),
            EditorFontSize,
            TextBrush,
            TextAlignment.Left,
            TextWrapping.NoWrap);
        return layout.WidthIncludingTrailingWhitespace;
    }

    private FormattedText Format(string text, IBrush brush)
        => new FormattedText(
            text,
            CultureInfo.CurrentUICulture,
            FlowDirection.LeftToRight,
            new Typeface(EditorFontFamily, FontStyle.Normal, FontWeight.Normal, FontStretch.Normal),
            EditorFontSize,
            brush);
}
