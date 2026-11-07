using System;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using TouchKeyboard.Core;
using AvaloniaPath = Avalonia.Controls.Shapes.Path;

namespace TouchKeyboard.Avalonia;

/// <summary>
/// Window-level overlay: scrim, panel, title row, editor surface and key layout.
/// Place it last in the window's root grid; AutoAttach wires the pointer
/// controller and the controller restores focus when the panel closes.
/// </summary>
public sealed class TouchKeyboardHost : UserControl
{
    private readonly Panel _root;
    private readonly Border _scrim;
    private readonly Border _panel;
    private readonly TextBlock _labelText;
    private readonly TextBlock _unitText;
    private readonly TextBlock _errorText;
    private readonly TouchButton _revealButton;
    private readonly TouchButton _closeButton;
    private readonly TextBlock _closeGlyph;
    private readonly AvaloniaPath _revealIcon;
    private readonly Viewbox _revealIconHost;
    private readonly Border _editorBorder;
    private readonly ContentControl _editorHost;
    private readonly Border _keyboardArea;
    private readonly KeyboardLayoutView _layoutView;

    private TouchKeyboardController? _controller;
    private KeyboardSession? _session;
    private IEditorAdapter? _adapter;
    private IEditorSurface? _editorSurface;
    private ITouchKeyboardLocalizer _localizer = TouchKeyboardLocalizer.Default;
    private TouchKeyboardTheme _theme = TouchKeyboardTheme.Light;
    private TopLevel? _attachedTopLevel;

    /// <summary>Creates the host with its default layout.</summary>
    public TouchKeyboardHost()
    {
        Focusable = true;
        IsVisible = false;
        HorizontalAlignment = HorizontalAlignment.Stretch;
        VerticalAlignment = VerticalAlignment.Stretch;
        ClipToBounds = true;

        _root = new Panel();
        _scrim = new Border { Background = TouchKeyboardTheme.Light.Scrim };
        _scrim.PointerPressed += OnScrimPressed;

        _panel = new Border
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Bottom,
            Margin = new Thickness(16, 0, 16, 10),
            CornerRadius = new CornerRadius(TouchKeyboardTheme.Light.PanelCornerRadius),
            Padding = new Thickness(TouchKeyboardTheme.Light.PanelPadding),
            MaxWidth = 1280
        };

        _labelText = new TextBlock
        {
            FontSize = TouchKeyboardTheme.Light.TitleFontSize,
            FontWeight = FontWeight.Bold,
            Foreground = TouchKeyboardTheme.Light.Text,
            VerticalAlignment = VerticalAlignment.Center
        };
        _unitText = new TextBlock
        {
            FontSize = TouchKeyboardTheme.Light.UnitFontSize,
            Foreground = TouchKeyboardTheme.Light.MutedText,
            VerticalAlignment = VerticalAlignment.Center,
            IsVisible = false
        };
        _errorText = new TextBlock
        {
            FontSize = 14,
            Foreground = TouchKeyboardTheme.Light.ErrorText,
            TextTrimming = TextTrimming.CharacterEllipsis,
            TextAlignment = TextAlignment.Right,
            MaxWidth = 480,
            VerticalAlignment = VerticalAlignment.Center,
            IsVisible = false
        };

        _closeGlyph = new TextBlock
        {
            Text = "×",
            FontSize = 26,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        _closeButton = new TouchButton
        {
            Width = 48,
            Height = 48,
            Radius = 8
        };
        _closeButton.SetContent(_closeGlyph);
        _closeButton.Activated += OnCloseClick;

        _editorHost = new ContentControl
        {
            HorizontalContentAlignment = HorizontalAlignment.Stretch,
            VerticalContentAlignment = VerticalAlignment.Center
        };
        _editorBorder = new Border
        {
            MinHeight = 60,
            CornerRadius = new CornerRadius(8),
            BorderThickness = new Thickness(1),
            Padding = new Thickness(2),
            Child = _editorHost
        };

        _revealIcon = new AvaloniaPath
        {
            Stretch = Stretch.None,
            Width = TouchKeyboardGlyphs.DesignSize,
            Height = TouchKeyboardGlyphs.DesignSize,
            StrokeLineCap = PenLineCap.Round,
            StrokeJoin = PenLineJoin.Round
        };
        _revealIconHost = new Viewbox
        {
            Width = 30,
            Height = 30,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Stretch = Stretch.Uniform,
            Child = _revealIcon
        };
        _revealButton = new TouchButton
        {
            IsVisible = false,
            Height = 48,
            MinWidth = 72,
            Radius = 8
        };
        _revealButton.SetContent(_revealIconHost);
        _revealButton.Activated += OnRevealClick;

        _layoutView = new KeyboardLayoutView
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Center
        };
        _layoutView.KeyActivated += OnKeyActivated;

        _keyboardArea = new Border
        {
            VerticalAlignment = VerticalAlignment.Center,
            Child = _layoutView
        };

        BuildUi();
        Content = _root;
        ApplyTheme();
    }

    /// <summary>Options for the overlay, theme, key height and editor surface.</summary>
    public TouchKeyboardOptions Options { get; } = new TouchKeyboardOptions();

    /// <summary>Automatically create a controller when attached to a window.</summary>
    public bool AutoAttach { get; set; } = true;

    /// <summary>Whether the panel is open.</summary>
    public bool IsOpen => IsVisible;

    /// <summary>The current session, when open.</summary>
    public KeyboardSession? Session => _session;

    /// <summary>
    /// The theme in effect: <see cref="TouchKeyboardOptions.Theme"/> when set,
    /// otherwise the dark or light preset of the application's theme variant.
    /// </summary>
    public TouchKeyboardTheme ResolvedTheme => _theme;

    /// <summary>Raised after the panel closes, with the target to refocus.</summary>
    public event EventHandler<TouchKeyboardClosedEventArgs>? Closed;

    /// <summary>Applies options and rebuilds themed chrome.</summary>
    public void Configure(Action<TouchKeyboardOptions> configure)
    {
        if (configure is null)
        {
            throw new ArgumentNullException(nameof(configure));
        }

        configure(Options);
        _localizer = ResolveLocalizer();
        _layoutView.Localizer = _localizer;
        _theme = ResolveTheme();
        ApplyTheme();

        if (_session is not null)
        {
            UpdateFromState(_session.State);
        }
    }

    /// <summary>Opens the panel for a session and its target adapter.</summary>
    public void Open(KeyboardSession session, IEditorAdapter adapter)
    {
        if (session is null)
        {
            throw new ArgumentNullException(nameof(session));
        }

        if (adapter is null)
        {
            throw new ArgumentNullException(nameof(adapter));
        }

        if (IsVisible)
        {
            CloseCore(cancelled: true);
        }

        _session = session;
        _adapter = adapter;
        _localizer = ResolveLocalizer();
        _layoutView.Localizer = _localizer;

        _labelText.Text = string.IsNullOrWhiteSpace(session.Label)
            ? _localizer.Get("title.edit")
            : session.Label;
        _unitText.Text = session.Unit ?? string.Empty;
        _unitText.IsVisible = !string.IsNullOrWhiteSpace(session.Unit);
        _errorText.Text = string.Empty;
        _errorText.IsVisible = false;

        ApplyTheme();
        CreateEditorSurface(session);
        session.StateChanged += OnSessionChanged;
        UpdateFromState(session.State);

        IsVisible = true;
        Focus();
        Dispatcher.UIThread.Post(
            () =>
            {
                if (IsVisible)
                {
                    Focus();
                }
            },
            DispatcherPriority.Input);
    }

    /// <summary>Closes the panel and discards the session.</summary>
    public void Close() => CloseCore(cancelled: true);

    /// <inheritdoc />
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel is not null && !ReferenceEquals(_attachedTopLevel, topLevel))
        {
            _attachedTopLevel = topLevel;
            topLevel.ActualThemeVariantChanged += OnActualThemeVariantChanged;
        }

        // Pick up the host window's variant: a dark shell gets the dark keyboard.
        _theme = ResolveTheme();
        ApplyTheme();
        if (_session is not null)
        {
            UpdateFromState(_session.State);
        }

        if (AutoAttach && _controller is null && topLevel is not null)
        {
            _controller = new TouchKeyboardController(topLevel, this);
        }
    }

    /// <inheritdoc />
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        if (IsVisible)
        {
            CloseCore(cancelled: true);
        }

        if (_attachedTopLevel is not null)
        {
            _attachedTopLevel.ActualThemeVariantChanged -= OnActualThemeVariantChanged;
            _attachedTopLevel = null;
        }

        _controller?.Dispose();
        _controller = null;
        base.OnDetachedFromVisualTree(e);
    }

    /// <inheritdoc />
    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (_session is null || !Options.HandlePhysicalKeyboard)
        {
            base.OnKeyDown(e);
            return;
        }

        switch (e.Key)
        {
            case Key.Escape:
                Close();
                e.Handled = true;
                return;
            case Key.Enter:
                Confirm();
                e.Handled = true;
                return;
            case Key.Back:
                _session.Execute(KeyCommand.Backspace);
                e.Handled = true;
                return;
            case Key.Left:
                _session.Execute(KeyCommand.MoveCaretLeft);
                e.Handled = true;
                return;
            case Key.Right:
                _session.Execute(KeyCommand.MoveCaretRight);
                e.Handled = true;
                return;
            case Key.Up:
                _session.Execute(KeyCommand.StepDigitUp);
                e.Handled = true;
                return;
            case Key.Down:
                _session.Execute(KeyCommand.StepDigitDown);
                e.Handled = true;
                return;
            default:
                base.OnKeyDown(e);
                return;
        }
    }

    /// <inheritdoc />
    protected override void OnTextInput(TextInputEventArgs e)
    {
        if (_session is null || !Options.HandlePhysicalKeyboard)
        {
            base.OnTextInput(e);
            return;
        }

        if (!string.IsNullOrEmpty(e.Text))
        {
            _session.Execute(KeyCommand.Insert(e.Text));
            e.Handled = true;
        }
    }

    private void BuildUi()
    {
        var panelGrid = new Grid
        {
            RowDefinitions = new RowDefinitions("Auto,Auto,Auto"),
            RowSpacing = 10
        };

        var title = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,Auto,Auto"),
            ColumnSpacing = 8
        };

        var labelGroup = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 10,
            VerticalAlignment = VerticalAlignment.Center
        };
        labelGroup.Children.Add(_labelText);
        labelGroup.Children.Add(_unitText);

        Grid.SetColumn(labelGroup, 0);
        Grid.SetColumn(_errorText, 1);
        Grid.SetColumn(_closeButton, 2);
        title.Children.Add(labelGroup);
        title.Children.Add(_errorText);
        title.Children.Add(_closeButton);
        panelGrid.Children.Add(title);

        var editorRow = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,Auto"),
            ColumnSpacing = 8
        };
        Grid.SetColumn(_editorBorder, 0);
        Grid.SetColumn(_revealButton, 1);
        editorRow.Children.Add(_editorBorder);
        editorRow.Children.Add(_revealButton);
        Grid.SetRow(editorRow, 1);
        panelGrid.Children.Add(editorRow);

        Grid.SetRow(_keyboardArea, 2);
        panelGrid.Children.Add(_keyboardArea);

        _panel.Child = panelGrid;
        _root.Children.Add(_scrim);
        _root.Children.Add(_panel);
    }

    private void CreateEditorSurface(KeyboardSession session)
    {
        var factory = Options.EditorSurfaceFactory ?? (() => new TouchKeyboardEditor());
        _editorSurface = factory();

        var view = _editorSurface.View;
        view.HorizontalAlignment = HorizontalAlignment.Stretch;
        view.VerticalAlignment = VerticalAlignment.Center;

        if (_editorSurface is TouchKeyboardEditor editor)
        {
            editor.PlaceholderText = _localizer.Get("editor.placeholder");
            editor.TextBrush = _theme.Text;
            editor.MutedBrush = _theme.MutedText;
            editor.CaretBrush = _theme.Caret;
            editor.EditorFontSize = _theme.EditorFontSize;
            editor.RevealPassword = false;
        }

        _editorSurface.Attach(session);
        _editorHost.Content = view;
    }

    private void OnSessionChanged(object? sender, KeyboardStateChangedEventArgs e) => UpdateFromState(e.State);

    private void UpdateFromState(KeyboardState state)
    {
        if (_session is null)
        {
            return;
        }

        _layoutView.Localizer = _localizer;
        _layoutView.KeyboardTheme = _theme;
        _layoutView.KeyGap = _theme.KeyGap;
        UpdateKeyboardMetrics();
        _layoutView.Layout = state.Layout;
        _layoutView.ShiftActive = state.IsShiftActive;

        if (state.Mode == KeyboardMode.NumericKeypad)
        {
            _layoutView.Width = Math.Max(320, Options.NumericPanelMaxWidth);
            _layoutView.HorizontalAlignment = HorizontalAlignment.Center;
        }
        else
        {
            _layoutView.Width = double.NaN;
            _layoutView.HorizontalAlignment = HorizontalAlignment.Stretch;
        }

        _errorText.Text = state.Error is null ? string.Empty : _localizer.Format(state.Error);
        _errorText.IsVisible = state.Error is not null;

        var isPassword = _session.Profile.Id == "password";
        _revealButton.IsVisible = isPassword && _editorSurface is TouchKeyboardEditor;
        UpdateRevealIcon();
    }

    private void OnKeyActivated(object? sender, KeyActivatedEventArgs e)
    {
        if (_session is null)
        {
            return;
        }

        var command = _session.State.IsShiftActive && e.Key.ShiftedCommand.HasValue
            ? e.Key.ShiftedCommand.Value
            : e.Key.Command;

        switch (command.Kind)
        {
            case KeyCommandKind.Submit:
                Confirm();
                return;
            case KeyCommandKind.Close:
                Close();
                return;
            default:
                _session.Execute(command);
                return;
        }
    }

    private void Confirm()
    {
        if (_session is null || _adapter is null)
        {
            return;
        }

        if (!_session.TryCommit(out var text, out _))
        {
            return;
        }

        if (_adapter.TryCommit(text, out var error))
        {
            CloseCore(cancelled: false);
        }
        else
        {
            _session.ReportCommitFailure(
                error ?? new InputValidationError(InputValidationErrorCode.CommitRejected));
        }
    }

    private void CloseCore(bool cancelled)
    {
        if (_session is null)
        {
            IsVisible = false;
            return;
        }

        var session = _session;
        var target = _adapter?.Control;

        if (cancelled)
        {
            session.Cancel();
        }

        session.StateChanged -= OnSessionChanged;
        _editorSurface?.Detach();
        _editorSurface = null;
        _editorHost.Content = null;
        _session = null;
        _adapter = null;
        _layoutView.Layout = null;
        _errorText.IsVisible = false;
        IsVisible = false;

        Closed?.Invoke(this, new TouchKeyboardClosedEventArgs(!cancelled, target));
    }

    private void ApplyTheme()
    {
        var theme = _theme;

        _scrim.Background = theme.Scrim;
        _panel.Background = theme.PanelBackground;
        _panel.MaxWidth = Math.Max(320, Options.MaxPanelWidth);
        _panel.CornerRadius = new CornerRadius(theme.PanelCornerRadius);
        _panel.Padding = new Thickness(theme.PanelPadding);

        _labelText.Foreground = theme.Text;
        _labelText.FontSize = theme.TitleFontSize;
        _unitText.Foreground = theme.MutedText;
        _unitText.FontSize = theme.UnitFontSize;
        _errorText.Foreground = theme.ErrorText;

        _editorBorder.Background = theme.EditorBackground;
        _editorBorder.BorderBrush = theme.EditorBorder;

        _closeButton.Background = theme.DangerKeyBackground;
        _closeButton.PressedOverlay = theme.PressedOverlay;
        _closeButton.PressedScale = theme.PressedScale;
        _closeGlyph.Foreground = theme.DangerKeyForeground;
        _closeGlyph.Text = _localizer.Get("close");
        AutomationProperties.SetName(_closeButton, _localizer.Get("close.name"));

        _revealButton.Background = theme.ActionKeyBackground;
        _revealButton.PressedOverlay = theme.PressedOverlay;
        _revealButton.PressedScale = theme.PressedScale;
        UpdateRevealIcon();

        _layoutView.KeyboardTheme = theme;
        _layoutView.KeyGap = theme.KeyGap;

        if (_editorSurface is TouchKeyboardEditor editor)
        {
            editor.TextBrush = theme.Text;
            editor.MutedBrush = theme.MutedText;
            editor.CaretBrush = theme.Caret;
            editor.EditorFontSize = theme.EditorFontSize;
        }

        UpdateKeyboardMetrics();
    }

    private void UpdateKeyboardMetrics()
    {
        var keyHeight = ResolveKeyHeight();
        _layoutView.KeyHeight = keyHeight;
        _keyboardArea.Height = keyHeight * 5 + _theme.KeyGap * 4;
    }

    private double ResolveKeyHeight()
    {
        if (Options.KeyHeight > 0)
        {
            return Options.KeyHeight;
        }

        var height = TopLevel.GetTopLevel(this)?.Bounds.Height ?? 0;
        if (height > 0 && height < 640)
        {
            return 56;
        }

        if (height > 0 && height < 720)
        {
            return 64;
        }

        return 72;
    }

    private TouchKeyboardTheme ResolveTheme()
    {
        if (Options.Theme is { } explicitTheme)
        {
            return explicitTheme;
        }

        var variant = _attachedTopLevel?.ActualThemeVariant ?? Application.Current?.ActualThemeVariant;
        return variant == ThemeVariant.Dark ? TouchKeyboardTheme.Dark : TouchKeyboardTheme.Light;
    }

    private void OnActualThemeVariantChanged(object? sender, EventArgs e)
    {
        if (Options.Theme is not null)
        {
            return;
        }

        var resolved = ResolveTheme();
        if (ReferenceEquals(resolved, _theme))
        {
            return;
        }

        _theme = resolved;
        ApplyTheme();
        if (_session is not null)
        {
            UpdateFromState(_session.State);
        }
    }

    private void UpdateRevealIcon()
    {
        var revealed = _editorSurface is TouchKeyboardEditor { RevealPassword: true };
        var glyph = revealed ? KeyGlyph.HidePassword : KeyGlyph.ShowPassword;

        // Both states share one design canvas, so only the slash appears or
        // disappears; the eye itself never moves.
        _revealIconHost.IsVisible = TouchKeyboardGlyphs.Apply(_revealIcon, glyph, _theme.Text);
        AutomationProperties.SetName(
            _revealButton,
            _localizer.Get(revealed ? "editor.hide" : "editor.show"));
    }

    private ITouchKeyboardLocalizer ResolveLocalizer()
        => Options.Localizer
           ?? (string.IsNullOrWhiteSpace(Options.Culture)
               ? TouchKeyboardLocalizer.Default
               : TouchKeyboardLocalizer.Create(Options.Culture));

    private void OnScrimPressed(object? sender, PointerPressedEventArgs e)
    {
        e.Handled = true;
        Close();
    }

    private void OnCloseClick(object? sender, EventArgs e) => Close();

    private void OnRevealClick(object? sender, EventArgs e)
    {
        if (_editorSurface is not TouchKeyboardEditor editor)
        {
            return;
        }

        editor.RevealPassword = !editor.RevealPassword;
        UpdateRevealIcon();
    }
}

/// <summary>Event data for <see cref="TouchKeyboardHost.Closed"/>.</summary>
public sealed class TouchKeyboardClosedEventArgs : EventArgs
{
    /// <summary>Creates the event data.</summary>
    public TouchKeyboardClosedEventArgs(bool committed, Control? target)
    {
        Committed = committed;
        Target = target;
    }

    /// <summary>Whether the session was committed rather than discarded.</summary>
    public bool Committed { get; }

    /// <summary>The control to refocus, when it is still alive.</summary>
    public Control? Target { get; }
}
