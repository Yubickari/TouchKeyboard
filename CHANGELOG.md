# Changelog

All notable changes to this project are documented in this file.
The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

- P1 walking skeleton: solution, `TouchKeyboard.Core` and
  `TouchKeyboard.Avalonia` projects, demo app, core test project.
- Package metadata, central package versions, CI workflow and release preflight.
- Accepted decision record.
- P2 core: ten built-in `InputProfile` instances (text, password, ASCII token,
  signed/positive integer, signed/positive decimal, date `/`, date `.`, IPv4),
  immutable `KeyboardLayout` data, `KeyboardSession` (insert, backspace, clear,
  caret, per-digit step, sign, shift, mode switch, commit/cancel, max-length and
  range checks), culture-neutral validation errors and an input profile registry.
- P3 Avalonia frontend: overlay `TouchKeyboardHost` with scrim and title/unit/error
  row; auto-attaching `TouchKeyboardController` with target resolution and focus
  restore; custom-drawn `TouchKeyboardEditor` with caret, horizontal scroll and
  password mask; pointer-captured `TouchKey` with long-press repeat;
  `KeyboardLayoutView`; `IEditorSurface` injection; adapters for `TextBox`,
  `NumericUpDown` and `CalendarDatePicker`; custom adapter registry; light/dark
  themes; zh-CN/en-US localizer; physical keyboard support.
- Full-keyboard and Numpad redesign (P4-scale, see `docs/layouts.md`):
  `AnsiStyle`, the strict five-row 15-unit ANSI geometry with commandless Tab,
  Caps and IME spacer cells; the four-row `PhoneHybrid` letters keyboard with a
  full-keyboard-owned symbol page; the 5×4 slot-based `Numpad` with a real cell
  grid; `NumpadSlots`/`NumpadSlotKey` scenario mapping; `KeyboardGridLayout` and
  `KeyboardGridCell` with column/row spans; `KeyGlyph` semantic icons drawn as
  Avalonia vectors; `FullKeyboardPage` session state; and the retained `Sequence`
  numeric surface.
- Layout reference documentation and headless renders for the ANSI, PhoneHybrid,
  symbol-page, Numpad and dark-theme surfaces, plus an icon-variant sheet.
- Automatic light/dark theme following: the overlay resolves the preset from the
  host window's actual theme variant and re-applies on change; an explicit
  `TouchKeyboardOptions.Theme` pins one.
- `TouchButton`, a touch-first chrome button for the overlay header (no focus, no
  hover, strong press feedback, accessible name).
- `KeyGlyph.Space`, `KeyGlyph.ShowPassword`, `KeyGlyph.HidePassword` and
  `KeyGlyph.PlusMinus`; the space key, the password visibility toggle and the
  numeric ± slot now render vector icons.
- Demo selectors for the full-keyboard geometry (AnsiStyle / PhoneHybrid) and the
  app theme variant (system / light / dark).
- 101 core tests and 23 Avalonia headless tests per target framework (248
  executions), including the layout matrix, Numpad slot spans, symbol-page
  switching, dark-theme resolution, press feedback, icon rendering and
  accessible names.

### Changed

- `FullKeyboardLayoutKind.AnsiStrict` is renamed to `AnsiStyle`, implemented and
  made the default full keyboard. `PhoneHybrid` is now four letter rows with a
  symbol entry and no number row.
- `NumericKeypadLayoutKind.Phone` is renamed to `Sequence`. `Numpad` is the single
  active numeric surface; the header layout toggle is removed and no built-in
  profile enables `Sequence`.
- `NumericKeypadOptions` now feeds `NumpadSlots`; a missing decimal point or
  separator widens `0` to two columns and an empty right row-2 slot stretches
  Confirm to two rows instead of rendering disabled keys.
- Function keys (backspace, clear, enter, shift, caret and step arrows) render as
  vectors and expose localized accessible names; the caret and step label strings
  are now action names instead of glyph characters.
- Keyboard area keeps the fixed five-row height: five-row surfaces fill it and
  four-row surfaces centre in it, so the panel does not jump between modes.
- Keys and header chrome no longer use button hover states: a press paints a
  theme overlay that covers the whole cap and scales it down (`PressedOverlay`,
  `PressedScale`); the overlay sits outside the content margin so no unpainted
  border remains.
- Key labels get a themed optical offset (`KeyLabelOffset`) so digits and capitals
  sit on the visual centre instead of slightly above it.
- Caret keys use a filled triangle plus a caret bar and step keys pair a triangle
  with a `+`/`−` sign (the selected option A); shift, caret and step icons render
  at a smaller factor than the full-size glyphs. The backspace X moved left inside
  its tag.
- Glyphs share one 24×24 design canvas, so the space bar lines up with the eraser
  baseline and the password eye stays fixed while only its slash appears.
- PhoneHybrid geometry refined: every row spans ten units, the home row is inset
  half a key on both sides, and the bottom row is `符号 123 ← Space → IME Clear
  Confirm` with unit-width function keys and a 2.5-unit space.
- Rows now share one unit grid (gaps are half-margins inside the unit columns),
  so keys line up across rows; PhoneHybrid's `Z` sits directly under `S`.
- The custom editor measures text including trailing whitespace
  (`TextLayout.WidthIncludingTrailingWhitespace`), so the caret and the horizontal
  scroll visibly follow typed spaces instead of freezing after them.
- The numeric ± slot renders as a vector glyph sized with the digit keys instead
  of the smaller action font.
- The symbol page drops Clear and the numeric switch, flanks the space bar with
  caret keys, puts Backspace before Confirm and moves `~` to the end.

## [0.1.0-preview.1] - 2026-11-06

Not released yet.

[Unreleased]: https://github.com/Yubickari/TouchKeyboard/commits/main
[0.1.0-preview.1]: https://github.com/Yubickari/TouchKeyboard/releases/tag/v0.1.0-preview.1
