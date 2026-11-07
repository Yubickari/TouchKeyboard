using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using TouchKeyboard.Core;

namespace TouchKeyboard.Avalonia;

/// <summary>
/// Renders one immutable <see cref="KeyboardLayout"/> as rows of <see cref="TouchKey"/>
/// controls, or as a cell grid when the layout carries spans. Row-based rows scale
/// independently to the available width; grid columns share it equally.
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

        if (_layout.Grid is { } cellGrid)
        {
            _rows.Children.Add(BuildGrid(cellGrid));
        }
        else
        {
            foreach (var row in _layout.Rows)
            {
                _rows.Children.Add(BuildRow(row));
            }
        }

        InvalidateMeasure();
    }

    private Grid BuildRow(KeyboardRow row)
    {
        var grid = new Grid
        {
            Height = _keyHeight,
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
                var touchKey = CreateTouchKey(key);
                touchKey.Height = _keyHeight;

                // Gaps live inside the unit columns as half-margins, so every row
                // shares the same unit grid: PhoneHybrid's Z lands exactly on S.
                touchKey.Margin = new Thickness(_keyGap / 2, 0);

                Grid.SetColumn(touchKey, column);
                Grid.SetColumnSpan(touchKey, key.ColumnSpan);
                grid.Children.Add(touchKey);
            }

            column += key.ColumnSpan;
        }

        return grid;
    }

    /// <summary>
    /// Renders a cell grid: fixed row heights and equal star columns, with each key
    /// placed at its origin and spanning the cells it covers.
    /// </summary>
    private Grid BuildGrid(KeyboardGridLayout cellGrid)
    {
        var grid = new Grid
        {
            Height = cellGrid.Rows * _keyHeight + Math.Max(0, cellGrid.Rows - 1) * _keyGap,
            ColumnSpacing = _keyGap,
            RowSpacing = _keyGap,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };

        for (var column = 0; column < cellGrid.Columns; column++)
        {
            grid.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(1, GridUnitType.Star)));
        }

        for (var row = 0; row < cellGrid.Rows; row++)
        {
            grid.RowDefinitions.Add(new RowDefinition(new GridLength(_keyHeight)));
        }

        foreach (var cell in cellGrid.Cells)
        {
            if (cell.Key.Style == KeyStyle.Spacer)
            {
                continue;
            }

            var touchKey = CreateTouchKey(cell.Key);
            Grid.SetRow(touchKey, cell.Row);
            Grid.SetColumn(touchKey, cell.Column);
            Grid.SetRowSpan(touchKey, cell.RowSpan);
            Grid.SetColumnSpan(touchKey, cell.ColumnSpan);
            grid.Children.Add(touchKey);
        }

        return grid;
    }

    private TouchKey CreateTouchKey(KeyDefinition key)
    {
        var touchKey = new TouchKey(key, _theme, _localizer, _shiftActive)
        {
            IconSize = Math.Clamp(_keyHeight * 0.38, 18, 30)
        };
        touchKey.Activated += OnKeyActivated;
        return touchKey;
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
