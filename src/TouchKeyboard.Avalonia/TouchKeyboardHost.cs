using System;
using System.Globalization;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
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
    private const double CandidatePageButtonWidth = 56;
    private const double CandidatePageTextWidth = 44;

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
    private readonly Border _candidateBar;
    private readonly Grid _candidateGrid;
    private readonly StackPanel _candidateList;
    private readonly TouchButton _candidatePrevious;
    private readonly TouchButton _candidateNext;
    private readonly TextBlock _candidatePage;
    private readonly TextBlock _candidateHint;
    private readonly TextBlock _candidatePreviousGlyph;
    private readonly TextBlock _candidateNextGlyph;
    private readonly TouchButton[] _candidateButtons = new TouchButton[9];
    private readonly TextBlock[] _candidateNumbers = new TextBlock[9];
    private readonly TextBlock[] _candidateTexts = new TextBlock[9];
    private readonly TranslateTransform _panelTransform = new TranslateTransform();
    private readonly DispatcherTimer _panelDragTimer = new DispatcherTimer
    {
        Interval = TimeSpan.FromMilliseconds(420)
    };

    private TouchKeyboardController? _controller;
    private KeyboardSession? _session;
    private IEditorAdapter? _adapter;
    private IEditorSurface? _editorSurface;
    private ITouchKeyboardLocalizer _localizer = TouchKeyboardLocalizer.Default;
    private TouchKeyboardTheme _baseTheme = TouchKeyboardTheme.Light;
    private TouchKeyboardTheme _theme = TouchKeyboardTheme.Light;
    private TopLevel? _attachedTopLevel;
    private Grid? _titleRow;
    private Grid? _editorRow;
    private IPointer? _panelDragPointer;
    private Point _panelDragStartPoint;
    private double _panelDragStartOffsetX;
    private double _panelDragStartOffsetY;
    private double _panelOffsetX;
    private double _panelOffsetY;
    private bool _adjustingCandidatePageSize;
    private string? _lastCandidateInput;
    private double _lastCandidateRowWidth;
    private bool _panelDragPending;
    private bool _panelDragging;

    /// <summary>Creates the host with its default layout.</summary>
    public TouchKeyboardHost()
    {
        _baseTheme = TouchKeyboardTheme.Light;
        _theme = _baseTheme.ApplyStyle(Options.Style);
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

        _panel.RenderTransform = _panelTransform;
        _panel.RenderTransformOrigin = RelativePoint.TopLeft;
        _panel.SizeChanged += OnPanelSizeChanged;
        _panelDragTimer.Tick += OnPanelDragTimerTick;

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

        _candidateGrid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions(
                $"*,{CandidatePageButtonWidth},{CandidatePageButtonWidth},{CandidatePageTextWidth}"),
            ColumnSpacing = 6,
            VerticalAlignment = VerticalAlignment.Center
        };

        _candidateList = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 6,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Center
        };
        _candidateList.SizeChanged += OnCandidateListSizeChanged;
        Grid.SetColumn(_candidateList, 0);
        _candidateGrid.Children.Add(_candidateList);

        _candidateHint = new TextBlock
        {
            FontSize = 14,
            VerticalAlignment = VerticalAlignment.Center,
            TextTrimming = TextTrimming.CharacterEllipsis,
            IsVisible = false
        };
        Grid.SetColumn(_candidateHint, 0);
        _candidateGrid.Children.Add(_candidateHint);

        for (var index = 0; index < _candidateButtons.Length; index++)
        {
            var number = new TextBlock
            {
                FontSize = 12,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 4, 0)
            };
            var text = new TextBlock
            {
                FontSize = 18,
                VerticalAlignment = VerticalAlignment.Center,
                TextWrapping = TextWrapping.NoWrap
            };
            var content = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Center,
                // The press overlay must cover the whole cap, so inset the
                // content instead of using Border.Padding on the TouchButton.
                Margin = new Thickness(12, 2)
            };
            content.Children.Add(number);
            content.Children.Add(text);

            var button = new TouchButton
            {
                Height = 44,
                MinWidth = 44,
                IsVisible = false,
                Focusable = false
            };
            button.SetContent(content);
            var candidateIndex = index;
            button.Activated += (_, _) => SelectImeCandidate(candidateIndex);
            _candidateButtons[index] = button;
            _candidateNumbers[index] = number;
            _candidateTexts[index] = text;
            _candidateList.Children.Add(button);
        }

        _candidatePreviousGlyph = new TextBlock
        {
            Text = "‹",
            FontSize = 22,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        _candidatePrevious = new TouchButton
        {
            Width = CandidatePageButtonWidth,
            Height = 44,
            IsVisible = false
        };
        _candidatePrevious.SetContent(_candidatePreviousGlyph);
        _candidatePrevious.Activated += (_, _) => ChangeImePage(-1);
        Grid.SetColumn(_candidatePrevious, 1);
        _candidateGrid.Children.Add(_candidatePrevious);

        _candidateNextGlyph = new TextBlock
        {
            Text = "›",
            FontSize = 22,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        _candidateNext = new TouchButton
        {
            Width = CandidatePageButtonWidth,
            Height = 44,
            IsVisible = false
        };
        _candidateNext.SetContent(_candidateNextGlyph);
        _candidateNext.Activated += (_, _) => ChangeImePage(1);
        Grid.SetColumn(_candidateNext, 2);
        _candidateGrid.Children.Add(_candidateNext);

        _candidatePage = new TextBlock
        {
            FontSize = 12,
            Width = CandidatePageTextWidth,
            TextAlignment = TextAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            IsVisible = false,
            Margin = new Thickness(2, 0, 0, 0)
        };
        Grid.SetColumn(_candidatePage, 3);
        _candidateGrid.Children.Add(_candidatePage);

        _candidateBar = new Border
        {
            Height = TouchKeyboardTheme.Light.CandidateBarHeight,
            IsVisible = false,
            ClipToBounds = true,
            Child = _candidateGrid,
            Margin = new Thickness(0, 0, 0, 10)
        };

        BuildUi();
        Content = _root;
        SizeChanged += OnHostSizeChanged;
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

    /// <summary>The last 中/英 choice seen in this app session, if any.</summary>
    internal bool? LastImeEnabled { get; private set; }

    /// <summary>Test hook: the candidate bar row.</summary>
    internal Border CandidateBar => _candidateBar;

    /// <summary>Test hook: the panel border.</summary>
    internal Border Panel => _panel;

    /// <summary>Test hook: the title row that starts a long-press drag.</summary>
    internal Grid? TitleRow => _titleRow;

    /// <summary>Test hook: the candidate-list row (used to verify full labels fit).</summary>
    internal StackPanel CandidateList => _candidateList;

    /// <summary>Current horizontal panel offset after dragging.</summary>
    internal double PanelOffsetX => _panelOffsetX;

    /// <summary>Current vertical panel offset after dragging.</summary>
    internal double PanelOffsetY => _panelOffsetY;

    /// <summary>Test hook: whether a title long-press is waiting for the drag threshold.</summary>
    internal bool PanelDragPending => _panelDragPending;

    /// <summary>Test hook: whether the panel is currently being dragged.</summary>
    internal bool PanelDragging => _panelDragging;

    /// <summary>Test hook: starts drag mode without waiting for the long-press timer.</summary>
    internal void BeginPanelDragForTest()
    {
        if (_panelDragPointer is null || _titleRow is null)
        {
            return;
        }

        _panelDragTimer.Stop();
        _panelDragPending = false;
        _panelDragging = true;
        _panelDragPointer.Capture(_titleRow);
    }

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
        _baseTheme = ResolveBaseTheme();
        _theme = _baseTheme.ApplyStyle(Options.Style);
        ApplyTheme();

        if (_session is not null)
        {
            UpdateFromState(_session.State);
        }

        if (!Options.EnablePanelDrag)
        {
            ResetPanelPosition();
        }
        else
        {
            ClampPanelOffset();
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
        _baseTheme = ResolveBaseTheme();
        _theme = _baseTheme.ApplyStyle(Options.Style);
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

        CancelPanelDrag();
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
            RowDefinitions = new RowDefinitions("Auto,Auto,Auto,Auto"),
            RowSpacing = 0
        };

        var title = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,Auto,Auto"),
            ColumnSpacing = 8,
            Background = Brushes.Transparent
        };
        _titleRow = title;
        title.PointerPressed += OnTitlePointerPressed;
        title.PointerMoved += OnTitlePointerMoved;
        title.PointerReleased += OnTitlePointerReleased;
        title.PointerCaptureLost += OnTitlePointerCaptureLost;

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
        title.Margin = new Thickness(0, 0, 0, 10);
        title.Children.Add(labelGroup);
        title.Children.Add(_errorText);
        title.Children.Add(_closeButton);
        panelGrid.Children.Add(title);

        var editorRow = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,Auto"),
            ColumnSpacing = 8
        };
        _editorRow = editorRow;
        Grid.SetColumn(_editorBorder, 0);
        Grid.SetColumn(_revealButton, 1);
        editorRow.Children.Add(_editorBorder);
        editorRow.Children.Add(_revealButton);
        editorRow.Margin = new Thickness(0, 0, 0, 10);
        Grid.SetRow(editorRow, 1);
        panelGrid.Children.Add(editorRow);

        Grid.SetRow(_candidateBar, 2);
        panelGrid.Children.Add(_candidateBar);

        Grid.SetRow(_keyboardArea, 3);
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
            editor.CompositionTextBrush = _theme.CompositionText;
            editor.CompositionUnderlineBrush = _theme.CompositionUnderline;
            editor.CompositionUnderlineThickness = _theme.CompositionUnderlineThickness;
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
        UpdateLayoutMetrics(state);
        _layoutView.Layout = state.Layout;
        _layoutView.ShiftActive = state.IsShiftActive;

        if (state.Mode == KeyboardMode.NumericKeypad)
        {
            _layoutView.Width = Math.Max(320, Options.NumericPanelMaxWidth * StyleScale);
            _layoutView.HorizontalAlignment = HorizontalAlignment.Center;
        }
        else
        {
            _layoutView.Width = double.NaN;
            _layoutView.HorizontalAlignment = HorizontalAlignment.Stretch;
        }

        _errorText.Text = state.Error is null ? string.Empty : _localizer.Format(state.Error);
        _errorText.IsVisible = state.Error is not null;

        if (state.Ime is not null)
        {
            LastImeEnabled = state.Ime.IsEnabled;
        }

        UpdateCandidateBar(state);

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
        CancelPanelDrag();
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
        var scale = StyleScale;
        _panel.MaxWidth = Math.Max(320, Options.MaxPanelWidth * scale);
        _panel.Margin = new Thickness(
            Math.Max(8, 16 * scale),
            0,
            Math.Max(8, 16 * scale),
            Math.Max(6, 10 * scale));
        _panel.CornerRadius = new CornerRadius(theme.PanelCornerRadius);
        _panel.Padding = new Thickness(theme.PanelPadding);

        _labelText.Foreground = theme.Text;
        _labelText.FontSize = theme.TitleFontSize;
        _unitText.Foreground = theme.MutedText;
        _unitText.FontSize = theme.UnitFontSize;
        _errorText.Foreground = theme.ErrorText;

        _editorBorder.Background = theme.EditorBackground;
        _editorBorder.BorderBrush = theme.EditorBorder;

        var rowGap = 10 * scale;
        if (_titleRow is not null)
        {
            _titleRow.Margin = new Thickness(0, 0, 0, rowGap);
        }

        if (_editorRow is not null)
        {
            _editorRow.Margin = new Thickness(0, 0, 0, rowGap);
        }

        _closeButton.Width = Math.Max(44, 48 * scale);
        _closeButton.Height = Math.Max(44, 48 * scale);
        _closeButton.Radius = Math.Max(4, 8 * scale);
        _closeButton.Background = theme.DangerKeyBackground;
        _closeButton.PressedOverlay = theme.PressedOverlay;
        _closeButton.PressedScale = theme.PressedScale;
        _closeGlyph.Foreground = theme.DangerKeyForeground;
        _closeGlyph.FontSize = Math.Max(18, 26 * scale);
        _closeGlyph.Text = _localizer.Get("close");
        AutomationProperties.SetName(_closeButton, _localizer.Get("close.name"));

        _revealButton.Height = Math.Max(44, 48 * scale);
        _revealButton.MinWidth = Math.Max(44, 72 * scale);
        _revealButton.Radius = Math.Max(4, 8 * scale);
        _revealButton.Background = theme.ActionKeyBackground;
        _revealButton.PressedOverlay = theme.PressedOverlay;
        _revealButton.PressedScale = theme.PressedScale;
        _revealIconHost.Width = Math.Max(18, 30 * scale);
        _revealIconHost.Height = Math.Max(18, 30 * scale);
        _editorBorder.MinHeight = Math.Max(44, 60 * scale);
        _editorBorder.Padding = new Thickness(Math.Max(1, 2 * scale));
        UpdateRevealIcon();

        _layoutView.KeyboardTheme = theme;
        _layoutView.KeyGap = theme.KeyGap;

        if (_editorSurface is TouchKeyboardEditor editor)
        {
            editor.TextBrush = theme.Text;
            editor.MutedBrush = theme.MutedText;
            editor.CaretBrush = theme.Caret;
            editor.EditorFontSize = theme.EditorFontSize;
            editor.CompositionTextBrush = theme.CompositionText;
            editor.CompositionUnderlineBrush = theme.CompositionUnderline;
            editor.CompositionUnderlineThickness = theme.CompositionUnderlineThickness;
        }

        ApplyCandidateTheme(theme);
        UpdateLayoutMetrics(_session?.State);
    }

    private void UpdateLayoutMetrics(KeyboardState? state)
    {
        var keyHeight = ResolveKeyHeight();
        var rows = ResolveKeyboardRows(state);
        _layoutView.KeyHeight = keyHeight;
        _keyboardArea.Height = keyHeight * rows + _theme.KeyGap * Math.Max(0, rows - 1);
        UpdateCandidateMetrics(state, keyHeight);
    }

    /// <summary>
    /// Number of key rows the keyboard area must reserve for the current state.
    /// PhoneHybrid keeps its four key rows and lets the candidate row act as the
    /// first of the five rows used by the symbol page; numeric-only profiles use
    /// a compact four-row area.
    /// </summary>
    private int ResolveKeyboardRows(KeyboardState? state)
    {
        if (state is null)
        {
            return 5;
        }

        var allowsFullKeyboard = _session?.Profile.AllowsFullKeyboard ?? true;
        if (!allowsFullKeyboard)
        {
            // A profile-restricted numeric keypad never switches to the full
            // keyboard, so it can drop the unused fifth row.
            return Math.Max(4, state.Layout.RowCount);
        }

        if (state.Mode == KeyboardMode.NumericKeypad)
        {
            // General numeric from a PhoneHybrid text field uses the candidate
            // row as its first row, so it also stays at four key rows. The
            // retained five-row Sequence surface keeps its rows rather than
            // clipping; it is not enabled by any built-in profile.
            return state.Ime is not null
                   && state.FullKeyboardLayout == FullKeyboardLayoutKind.PhoneHybrid
                   && state.NumericKeypadLayout == NumericKeypadLayoutKind.Numpad
                ? 4
                : Math.Max(5, state.Layout.RowCount);
        }

        if (state.FullKeyboardLayout == FullKeyboardLayoutKind.PhoneHybrid)
        {
            if (state.Page == FullKeyboardPage.Symbols)
            {
                return 5;
            }

            // IME main page: candidate row + four key rows = five-row height.
            // No IME: keep the five-row area so main and symbol page do not jump.
            return state.Ime is not null ? 4 : 5;
        }

        return 5;
    }

    private void UpdateCandidateMetrics(KeyboardState? state, double keyHeight)
    {
        var show = ShouldShowCandidateBar(state);
        _candidateBar.IsVisible = show;
        if (!show)
        {
            return;
        }

        var firstRow = IsCandidateFirstRow(state);
        var height = firstRow ? keyHeight : Math.Max(44, _theme.CandidateBarHeight);
        var gap = firstRow ? _theme.KeyGap : 10 * StyleScale;
        _candidateBar.Height = height;
        _candidateBar.Margin = new Thickness(0, 0, 0, gap);

        foreach (var button in _candidateButtons)
        {
            button.Height = height;
        }

        _candidatePrevious.Height = height;
        _candidateNext.Height = height;
    }

    private bool ShouldShowCandidateBar(KeyboardState? state)
    {
        if (state?.Ime is null)
        {
            return false;
        }

        // The symbol page has no IME slot; hiding the row keeps its five key rows
        // the same total height as the main page's candidate row + four key rows.
        return state.Mode != KeyboardMode.FullKeyboard || state.Page != FullKeyboardPage.Symbols;
    }

    private bool IsCandidateFirstRow(KeyboardState? state)
    {
        if (!ShouldShowCandidateBar(state) || state is null)
        {
            return false;
        }

        if (state.Mode == KeyboardMode.NumericKeypad
            && state.NumericKeypadLayout != NumericKeypadLayoutKind.Numpad)
        {
            return false;
        }

        return state.FullKeyboardLayout == FullKeyboardLayoutKind.PhoneHybrid;
    }

    private double ResolveKeyHeight()
    {
        double height;
        if (Options.KeyHeight > 0)
        {
            height = Options.KeyHeight;
        }
        else
        {
            var windowHeight = TopLevel.GetTopLevel(this)?.Bounds.Height ?? 0;
            height = windowHeight > 0 && windowHeight < 640
                ? 56
                : windowHeight > 0 && windowHeight < 720
                    ? 64
                    : 72;
        }

        return Math.Max(44, height * StyleScale);
    }

    private TouchKeyboardTheme ResolveBaseTheme()
    {
        if (Options.Theme is { } explicitTheme)
        {
            return explicitTheme;
        }

        var variant = _attachedTopLevel?.ActualThemeVariant ?? Application.Current?.ActualThemeVariant;
        return variant == ThemeVariant.Dark ? TouchKeyboardTheme.Dark : TouchKeyboardTheme.Light;
    }

    private double StyleScale => Options.Style.EffectiveScale;

    private void OnActualThemeVariantChanged(object? sender, EventArgs e)
    {
        if (Options.Theme is not null)
        {
            return;
        }

        var resolved = ResolveBaseTheme();
        if (ReferenceEquals(resolved, _baseTheme))
        {
            return;
        }

        _baseTheme = resolved;
        _theme = _baseTheme.ApplyStyle(Options.Style);
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

    private void ApplyCandidateTheme(TouchKeyboardTheme theme)
    {
        _candidateBar.Height = Math.Max(44, theme.CandidateBarHeight);
        _candidateBar.Margin = new Thickness(0, 0, 0, 10);
        _candidateBar.Background = Brushes.Transparent;
        AutomationProperties.SetName(_candidateBar, _localizer.Get("ime.candidates"));

        foreach (var button in _candidateButtons)
        {
            button.Height = Math.Max(44, theme.CandidateBarHeight);
            button.Width = double.NaN;
            button.MinWidth = 44;
            button.Radius = theme.CandidateCornerRadius;
            button.PressedOverlay = theme.PressedOverlay;
            button.PressedScale = theme.PressedScale;
        }

        foreach (var number in _candidateNumbers)
        {
            number.Foreground = theme.CandidateMutedText;
        }

        foreach (var text in _candidateTexts)
        {
            text.FontSize = theme.CandidateFontSize;
            text.Foreground = theme.CandidateText;
        }

        _candidatePrevious.Height = Math.Max(44, theme.CandidateBarHeight);
        _candidateNext.Height = Math.Max(44, theme.CandidateBarHeight);
        _candidatePrevious.Width = CandidatePageButtonWidth;
        _candidateNext.Width = CandidatePageButtonWidth;
        _candidatePrevious.Radius = theme.CandidateCornerRadius;
        _candidateNext.Radius = theme.CandidateCornerRadius;
        _candidatePrevious.Background = theme.CandidateBackground;
        _candidateNext.Background = theme.CandidateBackground;
        _candidatePrevious.PressedOverlay = theme.PressedOverlay;
        _candidateNext.PressedOverlay = theme.PressedOverlay;
        _candidatePrevious.PressedScale = theme.PressedScale;
        _candidateNext.PressedScale = theme.PressedScale;
        _candidatePreviousGlyph.Foreground = theme.CandidateText;
        _candidateNextGlyph.Foreground = theme.CandidateText;
        _candidatePage.Foreground = theme.CandidateMutedText;
        _candidateHint.Foreground = theme.CandidateMutedText;
        AutomationProperties.SetName(_candidatePrevious, _localizer.Get("ime.previous"));
        AutomationProperties.SetName(_candidateNext, _localizer.Get("ime.next"));

    }

    private void UpdateCandidateBar(KeyboardState state)
    {
        var ime = state.Ime;
        if (ime is null || !ShouldShowCandidateBar(state))
        {
            HideCandidateContent();
            return;
        }

        if (state.Mode == KeyboardMode.NumericKeypad)
        {
            HideCandidateContent();
            return;
        }

        if (ime.IsComposing)
        {
            if (ime.Candidates.Count == 0)
            {
                HideCandidateButtons();
                _candidateList.IsVisible = false;
                _candidatePrevious.IsVisible = false;
                _candidateNext.IsVisible = false;
                _candidatePage.IsVisible = false;
                _lastCandidateInput = null;
                ShowCandidateHint(_localizer.Get("ime.no-candidate"));
                return;
            }

            _candidateHint.IsVisible = false;
            _candidateList.IsVisible = true;

            if (!string.Equals(_lastCandidateInput, ime.Input, StringComparison.Ordinal))
            {
                _lastCandidateInput = ime.Input;
                if (ime.PageSize < Options.Ime.CandidatePageSize)
                {
                    _session?.SetImeCandidatePageSize(Options.Ime.CandidatePageSize);
                    return;
                }
            }

            for (var index = 0; index < _candidateButtons.Length; index++)
            {
                var visible = index < ime.Candidates.Count;
                var button = _candidateButtons[index];
                button.IsVisible = visible;
                if (!visible)
                {
                    continue;
                }

                var candidate = ime.Candidates[index];
                var selected = index == ime.SelectedIndex;
                _candidateNumbers[index].Text = (index + 1).ToString(CultureInfo.InvariantCulture);
                _candidateTexts[index].Text = candidate.Text;
                _candidateTexts[index].Foreground = selected
                    ? _theme.CandidateSelectedText
                    : _theme.CandidateText;
                button.Background = selected
                    ? _theme.CandidateSelectedBackground
                    : _theme.CandidateBackground;
                AutomationProperties.SetName(
                    button,
                    string.Format(
                        CultureInfo.CurrentCulture,
                        _localizer.Get("ime.candidate.name"),
                        index + 1,
                        candidate.Text));
            }

            _candidatePrevious.IsVisible = true;
            _candidateNext.IsVisible = true;
            _candidatePrevious.IsEnabled = ime.HasPreviousPage;
            _candidateNext.IsEnabled = ime.HasNextPage;
            _candidatePage.Text = string.Format(
                CultureInfo.CurrentCulture,
                _localizer.Get("ime.page"),
                ime.PageIndex + 1,
                ime.PageCount);
            _candidatePage.IsVisible = true;
            RedistributeCandidateWidths();
            return;
        }

        HideCandidateButtons();
        _candidateList.IsVisible = false;
        _candidatePrevious.IsVisible = false;
        _candidateNext.IsVisible = false;
        _candidatePage.IsVisible = false;
        _lastCandidateInput = null;
        ShowCandidateHint(_localizer.Get(ime.IsEnabled ? "ime.hint.chinese" : "ime.hint.english"));
    }

    private void HideCandidateContent()
    {
        HideCandidateButtons();
        _candidateList.IsVisible = false;
        _candidateHint.IsVisible = false;
        _candidatePrevious.IsVisible = false;
        _candidateNext.IsVisible = false;
        _candidatePage.IsVisible = false;
        _lastCandidateInput = null;
    }

    private void HideCandidateButtons()
    {
        foreach (var button in _candidateButtons)
        {
            button.IsVisible = false;
        }
    }

    private void ShowCandidateHint(string text)
    {
        _candidateHint.Text = text;
        _candidateHint.IsVisible = true;
    }

    private void OnCandidateListSizeChanged(object? sender, SizeChangedEventArgs e)
        => RedistributeCandidateWidths();

    /// <summary>
    /// Measures every candidate at its complete text width. The row shows as many
    /// as fit (up to nine); if the current page is too wide it asks Core for a
    /// smaller page size, so paging is the only overflow mechanism. Candidate keys
    /// always keep their natural width instead of stretching to fill the row, even
    /// when only two or three candidates are visible.
    /// </summary>
    private void RedistributeCandidateWidths()
    {
        var ime = _session?.State.Ime;
        if (ime is null || !ime.IsComposing || ime.Candidates.Count == 0 || !_candidateList.IsVisible)
        {
            return;
        }

        var count = Math.Min(ime.Candidates.Count, _candidateButtons.Length);
        var available = _candidateList.Bounds.Width;
        if (count == 0 || available <= 0)
        {
            return;
        }

        if (available > _lastCandidateRowWidth + 1
            && ime.PageSize < Options.Ime.CandidatePageSize
            && !_adjustingCandidatePageSize)
        {
            _lastCandidateRowWidth = available;
            _adjustingCandidatePageSize = true;
            try
            {
                _session?.SetImeCandidatePageSize(Options.Ime.CandidatePageSize);
            }
            finally
            {
                _adjustingCandidatePageSize = false;
            }

            Dispatcher.UIThread.Post(RedistributeCandidateWidths, DispatcherPriority.Loaded);
            return;
        }

        _lastCandidateRowWidth = available;

        var desired = new double[count];
        var used = 0.0;
        var fit = 0;
        for (var index = 0; index < count; index++)
        {
            var button = _candidateButtons[index];
            button.Width = double.NaN;
            button.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            desired[index] = Math.Max(44, button.DesiredSize.Width);

            var next = used
                       + (fit > 0 ? _candidateList.Spacing : 0)
                       + desired[index];
            if (next > available)
            {
                break;
            }

            used = next;
            fit++;
        }

        fit = Math.Max(1, Math.Min(fit, 9));
        if (count > 1 && ime.PageSize > fit)
        {
            if (_adjustingCandidatePageSize)
            {
                return;
            }

            _adjustingCandidatePageSize = true;
            try
            {
                _session?.SetImeCandidatePageSize(fit);
            }
            finally
            {
                _adjustingCandidatePageSize = false;
            }

            Dispatcher.UIThread.Post(RedistributeCandidateWidths, DispatcherPriority.Loaded);
            return;
        }

        for (var index = 0; index < count; index++)
        {
            _candidateButtons[index].Width = desired[index];
        }
    }

    private void SelectImeCandidate(int index) => _session?.SelectImeCandidate(index);

    private void ChangeImePage(int delta) => _session?.ChangeImePage(delta);

    /// <summary>
    /// Moves the panel back to its default bottom-centre position. Call this after
    /// the host or the window size changes if the host keeps its own layout state.
    /// </summary>
    public void ResetPanelPosition()
    {
        CancelPanelDrag();
        _panelOffsetX = 0;
        _panelOffsetY = 0;
        ApplyPanelOffset();
    }

    private void OnHostSizeChanged(object? sender, SizeChangedEventArgs e)
        => ClampPanelOffset();

    private void OnPanelSizeChanged(object? sender, SizeChangedEventArgs e)
        => ClampPanelOffset();

    private void ApplyPanelOffset()
    {
        _panelTransform.X = _panelOffsetX;
        _panelTransform.Y = _panelOffsetY;
    }

    private void ClampPanelOffset()
    {
        if (!Options.EnablePanelDrag)
        {
            ResetPanelPosition();
            return;
        }

        var bounds = GetPanelDragBounds();
        if (bounds is null)
        {
            ApplyPanelOffset();
            return;
        }

        _panelOffsetX = Math.Clamp(_panelOffsetX, bounds.Value.MinX, bounds.Value.MaxX);
        _panelOffsetY = Math.Clamp(_panelOffsetY, bounds.Value.MinY, bounds.Value.MaxY);
        ApplyPanelOffset();
    }

    private (double MinX, double MaxX, double MinY, double MaxY)? GetPanelDragBounds()
    {
        var root = _root.Bounds;
        var panel = _panel.Bounds;
        if (root.Width <= 0 || root.Height <= 0 || panel.Width <= 0 || panel.Height <= 0)
        {
            return null;
        }

        // Bounds is the layout position relative to the root panel and is not
        // affected by the render transform, so it gives the exact clamp origin
        // regardless of stretch/max-width alignment.
        var baseLeft = panel.X;
        var baseTop = panel.Y;
        return (
            -baseLeft,
            Math.Max(-baseLeft, root.Width - baseLeft - panel.Width),
            -baseTop,
            Math.Max(-baseTop, root.Height - baseTop - panel.Height));
    }

    private bool HasPanelDragRoom()
    {
        var bounds = GetPanelDragBounds();
        if (bounds is null)
        {
            return false;
        }

        return bounds.Value.MaxX - bounds.Value.MinX > 0.5
               || bounds.Value.MaxY - bounds.Value.MinY > 0.5;
    }

    private void OnTitlePointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!Options.EnablePanelDrag
            || _titleRow is null
            || IsInsideCloseButton(e.Source)
            || !HasPanelDragRoom())
        {
            return;
        }

        var point = e.GetCurrentPoint(_titleRow);
        if (!point.Properties.IsLeftButtonPressed)
        {
            return;
        }

        CancelPanelDrag();
        _panelDragPointer = e.Pointer;
        _panelDragStartPoint = e.GetPosition(_root);
        _panelDragStartOffsetX = _panelOffsetX;
        _panelDragStartOffsetY = _panelOffsetY;
        _panelDragPending = true;
        _panelDragTimer.Start();
    }

    private void OnTitlePointerMoved(object? sender, PointerEventArgs e)
    {
        if (_panelDragPending)
        {
            var point = e.GetPosition(_root);
            if (Distance(point, _panelDragStartPoint) > 8)
            {
                CancelPanelDrag();
            }

            return;
        }

        if (!_panelDragging)
        {
            return;
        }

        var current = e.GetPosition(_root);
        var bounds = GetPanelDragBounds();
        if (bounds is null)
        {
            return;
        }

        _panelOffsetX = Math.Clamp(
            _panelDragStartOffsetX + (current.X - _panelDragStartPoint.X),
            bounds.Value.MinX,
            bounds.Value.MaxX);
        _panelOffsetY = Math.Clamp(
            _panelDragStartOffsetY + (current.Y - _panelDragStartPoint.Y),
            bounds.Value.MinY,
            bounds.Value.MaxY);
        ApplyPanelOffset();
        e.Handled = true;
    }

    private void OnTitlePointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        var wasDragging = _panelDragging;
        CancelPanelDrag();
        if (wasDragging)
        {
            e.Handled = true;
        }
    }

    private void OnTitlePointerCaptureLost(object? sender, PointerCaptureLostEventArgs e)
        => CancelPanelDrag();

    private void OnPanelDragTimerTick(object? sender, EventArgs e)
    {
        if (!_panelDragPending || _panelDragPointer is null || _titleRow is null)
        {
            CancelPanelDrag();
            return;
        }

        _panelDragTimer.Stop();
        _panelDragPending = false;
        _panelDragging = true;
        _panelDragPointer.Capture(_titleRow);
    }

    private void CancelPanelDrag()
    {
        _panelDragTimer.Stop();
        _panelDragPending = false;
        _panelDragging = false;
        var pointer = _panelDragPointer;
        _panelDragPointer = null;
        pointer?.Capture(null);
    }

    private bool IsInsideCloseButton(object? source)
    {
        if (source is not Visual visual)
        {
            return false;
        }

        for (Visual? current = visual; current is not null; current = current.GetVisualParent())
        {
            if (ReferenceEquals(current, _closeButton))
            {
                return true;
            }

            if (ReferenceEquals(current, _root))
            {
                break;
            }
        }

        return false;
    }

    private static double Distance(Point left, Point right)
    {
        var dx = left.X - right.X;
        var dy = left.Y - right.Y;
        return Math.Sqrt(dx * dx + dy * dy);
    }

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
