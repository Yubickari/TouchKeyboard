using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using TouchKeyboard.Core;

namespace TouchKeyboard.Avalonia;

/// <summary>
/// Renders one immutable <see cref="KeyboardLayout"/> as rows of <see cref="TouchKey"/>
/// controls. Each row scales to the available width; row totals may differ.
/// </summary>
public sealed class KeyboardLayoutView : UserControl
{
    private readonly StackPanel _rows;
    private KeyboardLayout? _layout;
    private TouchKeyboardTheme _theme = TouchKeyboardTheme.Light;
    private ITouchKeyboardLocalizer _localizer = TouchKeyboardLocalizer.Default;
    private bool _shiftActive;
    private double _keyHeight = 64;
    private double _keyGap = 6;

    /// <summary>Creates the layout view.</summary>
    public KeyboardLayoutView()
    {
        _rows = new StackPanel { Orientation = Orientation.Vertical };
        Content = _rows;
    }

    /// <summary>Raised when a key is activated.</summary>
    public event EventHandler<KeyActivatedEventArgs>? KeyActivated;

    /// <summary>The layout to render.</summary>
    public KeyboardLayout? Layout
    {
        get => _layout;
        set
        {
            if (ReferenceEquals(_layout, value))
            {
                return;
            }

            _layout = value;
            Rebuild();
        }
    }

    /// <summary>Whether labels should show the shifted layer.</summary>
    public bool ShiftActive
    {
        get => _shiftActive;
        set
        {
            if (_shiftActive == value)
            {
                return;
            }

            _shiftActive = value;
            UpdateShift();
        }
    }

    /// <summary>The current theme.</summary>
    public TouchKeyboardTheme KeyboardTheme
    {
        get => _theme;
        set
        {
            if (ReferenceEquals(_theme, value))
            {
                return;
            }

            _theme = value ?? throw new ArgumentNullException(nameof(value));
            Rebuild();
        }
    }

    /// <summary>The localizer used for named keys.</summary>
    public ITouchKeyboardLocalizer Localizer
    {
        get => _localizer;
        set
        {
            if (ReferenceEquals(_localizer, value))
            {
                return;
            }

            _localizer = value ?? throw new ArgumentNullException(nameof(value));
            Rebuild();
        }
    }

    /// <summary>Height of one key row.</summary>
    public double KeyHeight
    {
        get => _keyHeight;
        set
        {
            if (value <= 0 || Math.Abs(_keyHeight - value) < 0.01)
            {
                return;
            }

            _keyHeight = value;
            Rebuild();
        }
    }

    /// <summary>Gap between keys and rows.</summary>
    public double KeyGap
    {
        get => _keyGap;
        set
        {
            var gap = Math.Max(0, value);
            if (Math.Abs(_keyGap - gap) < 0.01)
            {
                return;
            }

            _keyGap = gap;
            Rebuild();
        }
    }

    private void Rebuild()
    {
        _rows.Children.Clear();
        _rows.Spacing = _keyGap;

        if (_layout is null)
        {
            return;
        }

        foreach (var row in _layout.Rows)
        {
            var grid = new Grid
            {
                Height = _keyHeight,
                ColumnSpacing = _keyGap,
                HorizontalAlignment = HorizontalAlignment.Stretch
            };

            var column = 0;
            foreach (var key in row.Keys)
            {
                grid.ColumnDefinitions.Add(new ColumnDefinition(
                    new GridLength(key.WidthUnits, GridUnitType.Star)));

                if (key.Style == KeyStyle.Spacer)
                {
                    var spacer = new Border { Background = Brushes.Transparent };
                    Grid.SetColumn(spacer, column);
                    Grid.SetColumnSpan(spacer, key.ColumnSpan);
                    grid.Children.Add(spacer);
                }
                else
                {
                    var touchKey = new TouchKey(key, _theme, _localizer, _shiftActive)
                    {
                        Height = _keyHeight
                    };
                    touchKey.Activated += OnKeyActivated;
                    Grid.SetColumn(touchKey, column);
                    Grid.SetColumnSpan(touchKey, key.ColumnSpan);
                    grid.Children.Add(touchKey);
                }

                column += key.ColumnSpan;
            }

            _rows.Children.Add(grid);
        }

        InvalidateMeasure();
    }

    private void UpdateShift()
    {
        foreach (var child in _rows.Children)
        {
            if (child is not Grid grid)
            {
                continue;
            }

            foreach (var key in grid.Children)
            {
                if (key is TouchKey touchKey)
                {
                    touchKey.SetShift(_shiftActive);
                }
            }
        }
    }

    private void OnKeyActivated(object? sender, EventArgs e)
    {
        if (sender is TouchKey key)
        {
            KeyActivated?.Invoke(this, new KeyActivatedEventArgs(key.Definition));
        }
    }
}

/// <summary>Event data for <see cref="KeyboardLayoutView.KeyActivated"/>.</summary>
public sealed class KeyActivatedEventArgs : EventArgs
{
    /// <summary>Creates the event data.</summary>
    public KeyActivatedEventArgs(KeyDefinition key)
    {
        Key = key;
    }

    /// <summary>The activated key definition.</summary>
    public KeyDefinition Key { get; }
}
