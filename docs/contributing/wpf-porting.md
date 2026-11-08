# WPF frontend notes

The core is UI-free and targets `netstandard2.0`, so a WPF frontend can reuse it
without changes. WPF is **not implemented**; this note records the porting shape
and the traps to budget for.

## What the core already provides

- `InputProfile` / registry, validation errors, commit normalization.
- Immutable layouts (`KeyboardLayout`, `KeyboardRow`, `KeyboardGridLayout`), Numpad
  slots, symbol page and the retained Sequence surface.
- `KeyboardSession` (buffer, caret, mode, page, shift, commit/cancel) and immutable
  `KeyboardState` snapshots with `StateChanged`.
- `KeyCommand` with a closed `KeyCommandKind`, `KeyDefinition` including `KeyGlyph`.

A frontend only needs to: draw a snapshot, catch pointer input, map key presses to
`Execute`, provide a target adapter, and render the editor surface, theme and
localization.

## Suggested package

| Package | TFM | Notes |
|---|---|---|
| `TouchKeyboard.Wpf` | `net8.0-windows` / `net10.0-windows` | Same layering as the Avalonia package; `UseWPF` and no Avalonia reference. |

`TouchKeyboard.Core` stays `netstandard2.0` and keeps zero dependencies.

## Mapping table

| Avalonia piece | WPF equivalent |
|---|---|
| `UserControl` overlay in the window grid | `Grid` overlay placed last, `Panel.ZIndex` if needed |
| `PointerPressed` / capture | `MouseLeftButtonDown`, `Touch.Down`/`Stylus` events, `CaptureMouse()` / `Touch.Capture` |
| `DispatcherTimer` for long-press | `System.Windows.Threading.DispatcherTimer` |
| `Border` + `CornerRadius` | `Border` + `CornerRadius` |
| Press overlay layer + `ScaleTransform` | extra `Border` layer + `ScaleTransform` with `RenderTransformOrigin` |
| `Viewbox` + `Path` (shared 24×24 canvas) | `Viewbox` + `Path`; reuse the path strings from `TouchKeyboardGlyphs` |
| `Geometry.Parse` path data | `Geometry.Parse` (`System.Windows.Media`) |
| `TextBlock` labels | `TextBlock` |
| Custom-drawn `Control.OnRender` | `FrameworkElement.OnRender(DrawingContext)` |
| `FormattedText` text measurement | `FormattedText` (WPF exposes `WidthIncludingTrailingWhitespace`) |
| `AutomationProperties.Name` + peer | `AutomationProperties.Name` + `FrameworkElementAutomationPeer` |
| Attached properties | WPF attached dependency properties (`TouchKeyboardInput.Profile` etc.) |

## Porting steps

1. **Control metadata and adapters.** Recreate `TouchKeyboardInput` attached
   properties (`Profile`, `Label`, `Unit`, `Enabled`) and adapters for `TextBox`,
   `DatePicker` and custom controls. NumericUpDown is not built in; many WPF apps
   use MahApps/HandyControl or custom fields, so keep the adapter registry.
2. **Overlay + controller.** Watch `PreviewMouseDown` on the window, resolve the
   target, open the overlay, restore focus on close. Keep the same rule that
   clicks starting inside a `Button` do not open the keyboard.
3. **Touch keys.** A `Border`-based control with `Focusable = false`, pointer
   capture, a press overlay and a small scale-down, and a long-press repeat timer.
   Touch and stylus must be handled alongside mouse input.
4. **Layout view.** Rows scale independently; the grid layout uses `Grid` row/column
   spans. Keep the shared unit-grid trick (gaps as half-margins inside unit columns)
   so rows with different column counts line up.
5. **Editor surface.** Default to a custom-drawn single-line control, like
   `TouchKeyboardEditor`, so the platform text input does not open. Measure
   trailing whitespace for the caret and horizontal scroll; otherwise typed spaces
   will appear to freeze the caret.
6. **Theme and icons.** Map `TouchKeyboardTheme` to WPF brushes. Glyph path data is
   plain geometry: copy the shared 24×24 canvas definitions into the WPF frontend
   (or move them into a neutral data file when both frontends exist). Implement the
   press overlay/scale with the same metrics.
7. **Theme variant.** WPF has no cross-platform `ThemeVariant`. Detect the Windows
   app theme (registry `AppsUseLightTheme`) plus `SystemEvents.UserPreferenceChanged`,
   or let the host pass an explicit light/dark preset.
8. **Localization.** Implement `ITouchKeyboardLocalizer` (same interface) with the
   built-in zh-CN/en-US strings plus error formatting.
9. **Accessibility.** Provide an automation peer with a button role and localized
   names; the custom editor has no native text semantics, same trade-off as
   Avalonia.
10. **Tests.** WPF UI tests are heavier than Avalonia headless. Split pure rendering
    helpers (glyph geometry, layout math) into testable classes and add an STA-based
    interaction test project; core tests already cover the logic.

## Traps

- **System OSK:** a custom-drawn editor avoids the touch keyboard, but any real
  `TextBox` surface will trigger it. Document the trade-off described by the
  editor-surface decision.
- **Focus:** the overlay must take keyboard focus to handle Escape/Enter without
  leaking keys into the target; restore focus after closing.
- **Pointer capture with touch:** WPF touch promotion (`Touch` → stylus → mouse) can
  duplicate events; handle the promoted events once.
- **DPI:** enable per-monitor DPI awareness (`PerMonitorV2`) in the app manifest and
  test 125%/150%/200%.
- **Theme detection:** registry reads are Windows-version dependent; make the
  variant an explicit option fallback.
- **Icon drift:** two frontends must render the same `KeyGlyph` set; keep the canvas
  coordinates shared and add a parity test once both exist.

## Testing checklist for a WPF spike

- Open/confirm/cancel, focus restore, window deactivation.
- Full keyboard (both geometries), symbol page, Numpad profiles.
- Long-press backspace/arrows; press feedback on a touch panel.
- Dark preset, DPI scaling, `TextBox`/`DatePicker` adapters.
- No OSK popping up and no key leakage into the target control.
