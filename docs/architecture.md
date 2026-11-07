# Architecture

TouchKeyboard is split into a pure .NET Standard backend and per-UI frontends. The
core owns the keyboard model and the editing state machine; a frontend owns pixels,
pointer capture, focus and the bridge to the host application's controls.

```text
                 host application (Avalonia, later WPF)
                                │
                    IEditorAdapter / IEditorSurface
                                │
┌───────────────────── TouchKeyboard.Avalonia ─────────────────────┐
│ host overlay · layout view · touch keys · editor surface ·        │
│ theme · localizer · target resolution · controller                │
└───────────────────────────────┬──────────────────────────────────┘
                                │ KeyboardSession / KeyboardState / KeyCommand
┌───────────────────── TouchKeyboard.Core (netstandard2.0) ────────┐
│ profiles · validation · layouts · grid cells · session · state    │
└──────────────────────────────────────────────────────────────────┘
```

## Projects and target frameworks

| Project | TFM | Purpose |
|---|---|---|
| `TouchKeyboard.Core` | `netstandard2.0` | Model, layouts, session, validation. No package dependencies. |
| `TouchKeyboard.Avalonia` | `net8.0;net10.0` | Avalonia 12 frontend: overlay, keys, editor surface, adapters. |
| `TouchKeyboard.Demo` | `net8.0` | Sample window used for manual testing and screenshots. |
| `TouchKeyboard.Core.Tests` | `net8.0;net10.0` | Core unit tests, architecture test, layout matrix. |
| `TouchKeyboard.Avalonia.Tests` | `net8.0;net10.0` | Headless Avalonia interaction and render tests. |

The core is the compatibility floor and must stay UI-free. An architecture test
(`ArchitectureTests.Core_does_not_reference_any_ui_or_platform_assembly`) fails if
the core references Avalonia, WPF, WindowsBase, X11 or similar. The core also never
uses timers, dispatchers, fonts or brushes; long-press repeat, caret blink and
theming live in the frontend.

## Core domain model

### Modes, pages and geometries

- `KeyboardMode` — `FullKeyboard` or `NumericKeypad`.
- `FullKeyboardLayoutKind` — `AnsiStyle` (default, strict five-row 15-unit ANSI) or
  `PhoneHybrid` (four-row phone-style letters keyboard).
- `NumericKeypadLayoutKind` — `Numpad` (default, active) or `Sequence` (retained
  phone-style page, not enabled by any built-in profile).
- `FullKeyboardPage` — `Main` or `Symbols`. Only `PhoneHybrid` has a symbol page;
  the numeric keypad never enters it.

### Profiles

`InputProfile` is the contract for one field: default mode, whether the full
keyboard is allowed, numeric keypad options, intermediate-state rules, commit
validation and normalization. Built-in profiles and their rules live in
[profiles.md](profiles.md). `InputProfileRegistry` resolves profile ids
(case-insensitive) for `TouchKeyboardInput.Profile`.

### Layouts

A `KeyboardLayout` is immutable and describes a surface in one of two shapes:

- **Rows** — `KeyboardRow` lists `KeyDefinition`s with unit widths. Each row scales
  independently, so rows with different totals can share one panel.
- **Cell grid** — `KeyboardGridLayout` positions cells on a fixed columns × rows
  grid; `KeyboardGridCell` pins a key to a row/column origin and the key's
  `ColumnSpan`/`RowSpan` cover several cells. `Rows` is empty for grid layouts.

`KeyDefinition` carries a stable id, a `KeyLabel` (literal text or localization
resource key), optional shifted label/command, an optional `KeyGlyph` vector icon,
the command, unit width, cell spans, visual `KeyStyle` and repeatability.
`KeyboardLayouts` builds every built-in surface:

| Layout id | Built by | Shape |
|---|---|---|
| `full-ansi-style` | `KeyboardLayouts.Full(AnsiStyle)` | rows |
| `full-phone-hybrid` | `KeyboardLayouts.Full(PhoneHybrid)` | rows |
| `full-symbol-page` | `KeyboardLayouts.Full(PhoneHybrid, Symbols)` | rows |
| `numeric-general-numpad` | `KeyboardLayouts.GeneralNumeric(Numpad)` | grid |
| `numeric-general-sequence` | `KeyboardLayouts.GeneralNumeric(Sequence)` | rows |
| `numeric-profile-<id>` | `KeyboardLayouts.ProfileNumeric(profile)` | grid |

The Numpad grid is generated from `NumpadSlots` (derived from the profile's
`NumericKeypadOptions`): which semantic key fills the row-2 side slots and which
separator fills the bottom slot. Empty slots change the geometry instead of
producing disabled keys — `0` widens over an empty decimal slot and Confirm
stretches over an empty right-row-2 slot. See [layouts.md](layouts.md).

### Commands

`KeyCommand` is a UI-agnostic struct with a `KeyCommandKind` and optional text:
insert text, backspace, clear, move caret, step digit up/down, toggle sign, toggle
shift, switch to full keyboard / numeric keypad, switch full-keyboard page, submit
and close. A key dispatches its command; the frontend decides which commands are
host actions (submit, close) and which go to the session.

### Session and state

`KeyboardSession` owns the edit buffer, caret, mode, page, sticky shift, layout and
commit/cancel lifecycle. `Execute(KeyCommand)` mutates the session; `TryCommit`
validates and normalizes; `Cancel` discards. Every change raises `StateChanged`
with a new immutable `KeyboardState` snapshot (text, caret, mode, page, shift,
layout, error, dirty flag, cancelled flag). Frontends render snapshots only; they
never mutate session fields directly.

Layout selection inside a session:

```text
mode == FullKeyboard   -> KeyboardLayouts.Full(fullKeyboardLayout, page)
mode == NumericKeypad  -> profile.AllowsFullKeyboard
                            ? KeyboardLayouts.GeneralNumeric(numericKeypadLayout)
                            : KeyboardLayouts.ProfileNumeric(profile)
```

Editing rules that are easy to miss:

- Inserts are rejected when they would break `IsIntermediateValid` or exceed the
  effective maximum length (`min(profile limit, target limit)`).
- `StepDigitUp/Down` only changes the digit immediately left of the caret and is
  gated by `NumericKeypadOptions.ShowStepKeys`; the caret does not move.
- `ToggleSign` is gated by `NumericKeypadOptions.ShowSign` and by the target's
  `AllowsNegativeSign`; the caret is adjusted so it stays relative to the digits.
- Switching mode or page resets sticky shift; switching to the numeric keypad
  resets the page to `Main`.
- Closing always discards. Only a successful `TryCommit` produces text, and the
  frontend writes it back through `IEditorAdapter.TryCommit`.

### Validation

`InputProfile.TryValidate` returns normalized text or `InputValidationError`, a
culture-neutral code plus arguments. The frontend localizes the message. Range
checks (`Minimum`/`Maximum`) and numeric parsing are applied by the session because
those limits come from the target control, not from the profile. The full error
code list lives in [profiles.md](profiles.md#validation-errors).

## Frontend responsibilities (Avalonia)

| Component | Responsibility |
|---|---|
| `TouchKeyboardController` | Watches window pointer presses, resolves an editable target, builds the adapter and session, opens the host, restores focus on close, cancels on window deactivation. |
| `TouchKeyboardHost` | Overlay chrome: scrim, panel, title/unit/error row, close button, editor row, fixed five-row keyboard area, page/mode rendering, theme resolution. |
| `KeyboardLayoutView` | Renders a `KeyboardLayout` as rows or as a cell grid. Rows share one unit grid (gaps are half-margins inside unit columns). |
| `TouchKey` | Border-based key: no focus, pointer capture, strong press overlay + scale, long-press repeat, vector glyphs, accessible name/peer. |
| `TouchKeyboardEditor` | Default custom-drawn single-line editor surface: text, caret, horizontal scroll, password mask. Measures trailing whitespace so the caret follows spaces. |
| `TouchKeyboardTheme` | Brushes and metrics for panel, editor, keys, press feedback and label offset; `Light` and `Dark` presets. |
| `TouchKeyboardLocalizer` | Resolves key labels and formats core validation errors (zh-CN / en-US). |
| Adapters | Read target text/metadata and write committed text back (`TextBox`, `NumericUpDown`, `CalendarDatePicker`, custom). |

The host follows the window's actual theme variant by default; `TouchKeyboardOptions.Theme`
pins a preset. See [avalonia.md](avalonia.md) for integration details.

## Extension seams

| Need | Seam |
|---|---|
| New validation/profile | Subclass `InputProfile`, register with `InputProfileRegistry.Register`. |
| New numeric scenario | Build `NumericKeypadOptions`/`NumpadSlots` and call `KeyboardLayouts.Numpad(id, slots)`. |
| Host-specific control | Implement `IEditorAdapter` (or `IEditorAdapterFactory`) and register it. |
| Own editor widget | Implement `IEditorSurface` and inject it through `TouchKeyboardOptions.EditorSurfaceFactory`. |
| New UI stack | Build a frontend on the core; keep pointer, focus, fonts and brushes out of the core. |
| Custom icon set | Provide a new frontend renderer for `KeyGlyph`; the core only names icons. |

## Testing strategy

- Core unit tests cover profiles, layouts (row widths, grid spans), session
  behavior, validation and the architecture rule.
- Avalonia headless tests cover opening/closing, adapters, physical input, shift,
  mode/page switching, dark theme resolution, press feedback, icon rendering,
  accessible names, and Z-under-S alignment in PhoneHybrid.
- `build/preflight.sh` runs Release tests, packs both packages, checks package
  contents (dll/xml/README/icon) and runs the hygiene scan.
- `build/smoke-pack.sh` packs and consumes both packages from a local feed in
  throwaway Core and Avalonia projects.

## Known limitations

- v1 has no IME or Chinese input; profiles are ASCII/digit oriented. The IME slot
  is reserved as a spacer and `KeyGlyph.Ime` is reserved.
- The editor is single-line: no selection, clipboard, undo or multi-line layout.
- Editing happens in the session buffer; the target control is written only on
  confirm. Closing the panel always discards.
- No F-keys, numpad/NumLock block, Ctrl/Alt/Win combinations or system keys.
- WPF is not implemented yet; the core is designed so a second frontend can reuse
  it (see [wpf-porting.md](wpf-porting.md)).
