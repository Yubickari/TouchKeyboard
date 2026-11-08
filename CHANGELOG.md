# Changelog

All notable changes to this project are documented in this file.
The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.2.0] - 2026-11-08

The first release. The `0.1.0` line is not released.

### Added

- Optional managed pinyin IME: Core `IImeEngine`, `ImeCandidate`,
  `ImeCompositionState`, the internal `ImeComposition` state machine and
  `KeyboardState.Ime`; `InputProfile.AllowsIme`; `KeyCommandKind.ToggleIme`; the
  `ime.toggle` full-keyboard key; and session routing for composition, candidate
  selection/paging, Space/1–9/Enter commits, punctuation mapping, backspace,
  arrows, mode/page toggles, clear and cancel.
- `TouchKeyboard.Ime.Pinyin` (common lexicon, about 20k words) and
  `TouchKeyboard.Ime.Pinyin.Extended` (about 100k words), both `netstandard2.0`
  and dependent only on Core (the extended pack also on the common pack). The
  pinyin engine supports full pinyin, word/sentence segmentation and common
  initial abbreviations; the lexicons are embedded, deflate-compressed and
  loaded lazily.
- Reproducible dictionary build (`tools/build-pinyin-dict.py`) from Unihan
  `kMandarin` (Unicode License V3) and jieba `dict.txt` (MIT), with generated
  phrase readings, a project-owned polyphone override table, source snapshot
  hashes and full licence texts in each package's `NOTICE`.
- Avalonia IME UI: `TouchKeyboardImeOptions`, `TouchKeyboardOptions.Ime` and the
  one-line `UseIme(engine)`; the 中/英 key, fixed 44 px candidate bar,
  candidate/paging touch targets, inline composition text and underline,
  no-candidate/mode hints, candidate/paging accessibility names and zh-CN/en-US
  IME strings. Custom `IEditorSurface` implementations fall back to the candidate
  bar prefix without a contract change.
- No-IME layout baseline: the commandless `spacer.ime` is removed; Space expands
  to 7.0 units (`AnsiStyle`) or 3.5 (`PhoneHybrid`). With an engine and an
  IME-capable profile, Space returns to 5.5/2.5 and the slot holds `ime.toggle`.
- `docs/ime.md`, a Chinese `text` field in the demo wired to the pinyin engine,
  and the 0.2.0 device-checklist items for Chinese input, CJK fonts, OSK and
  1024×600.
- Demo manual-test controls: a collapsible, theme-aware appearance panel with
  full-keyboard geometry and theme selectors, overall/font/icon scale sliders,
  accent/confirm/selected-candidate/panel/key colour selectors, long-press drag
  toggle, reset-position and reset-appearance buttons, and a Chinese field for
  candidate/press-feedback checks.
- Candidate text is no longer ellipsized: candidate buttons size to their full
  label, every candidate keeps its natural width instead of stretching to fill the
  row (including a 2–3 candidate page), and long candidates such as “各个” /
  “各个方向” render completely.
  The row has no scrollbar: the Avalonia frontend shrinks the Core page size
  (1–9) to fit the available width and pages only through 56 px 切页 keys.
- Compact appearance API: `TouchKeyboardOptions.Style` with `Scale`, `FontScale`,
  `IconScale` and semantic `AccentColor` / `ConfirmColor` /
  `SelectedCandidateColor` / `PanelColor` / `KeyColor` / `TextColor` overrides;
  `TouchKeyboardTheme.IconScale`; and `EnablePanelDrag` plus
  `ResetPanelPosition()` for long-press title-row dragging clamped to the window.
- 135 core tests + 40 pinyin/extended tests + 48 Avalonia headless tests per
  target framework (446 executions), including the 129-entry golden set,
  composition/commit/cancel/paging, no-engine compatibility, IME-key visibility,
  fixed candidate-row geometry, dark theme, custom-editor fallback and 1024×600.
- Package smoke now covers Avalonia-only, Avalonia + Pinyin and Avalonia +
  Extended, verifies that Avalonia-only has no language-pack dependency, and runs
  a trimmed AOT publish that loads both dictionaries and commits a composition.

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
- Documentation set: architecture, Avalonia integration, input profiles, layout
  reference, roadmap, release runbook, WPF porting notes, four ADRs (core purity,
  TFM matrix, no-IME and managed pinyin IME) and a real-device acceptance checklist.
- Release preparation: `assets/icon.png` plus `PackageIcon` metadata, GitHub issue
  templates, a tag-driven release workflow with NuGet Trusted Publishing, and the
  package smoke test wired into CI and the release workflow.
- `build/preflight.sh` now checks the packaged icon and XML docs and refuses a tag
  release from a dirty working tree.
- Local development notes moved to the ignored `docs/local/` folder.
- Design plan and ADR 0004 for the optional managed pinyin IME, optional
  per-language/text packs and the one-line `UseIme` API.
- The earlier baseline had 101 core tests and 23 Avalonia headless tests per
  target framework (248 executions), including the layout matrix, Numpad slot
  spans, symbol-page switching, dark-theme resolution, press feedback, icon
  rendering and accessible names.

### Changed

- Decision record D12 (Chinese input) revised for the optional `0.2.0` managed
  pinyin IME; D17 (IME package layering), D18 (IME activation and profile gate) and
  D19 (no-IME Space expansion) added, and D7 simplified to the MIT/permissive-data
  licence policy. ADR 0003 is superseded by ADR 0004; `0.1.0` is not released and
  `0.2.0` is the first release.
- `Version` is `0.2.0`; the release now ships four packages (`Core`, `Avalonia`,
  `Ime.Pinyin`, `Ime.Pinyin.Extended`) with package-content, NOTICE/dictionary and
  AOT/trim checks.
- `0.2.0` no-IME layout: the full keyboards no longer render a commandless IME
  spacer; Space expands over the slot (AnsiStyle 7.0 units, PhoneHybrid 3.5). When
  an engine and an IME-capable profile are present, the `ime.toggle` key returns and
  Space goes back to 5.5/2.5; toggling 中/英 does not change the width.
- PhoneHybrid height model refined: the 26-key main page and general numeric page
  use the candidate row as their first of five rows, so the candidate row height
  equals the key height and the main page matches the five-row symbol page. The
  symbol page hides the candidate row (no IME slot); numeric-only profiles use a
  compact four-row keyboard area. Switching main/symbol/numeric on PhoneHybrid
  keeps the panel height stable.
- IME candidate buttons now paint the press overlay across the whole cap; their
  content is inset with an inner margin instead of `Border.Padding`, which previously
  left unpainted strips on the left and right.
- Licence policy: code MIT; optional language data uses only permissive or
  attribution licences with a short `NOTICE`; no share-alike/copyleft data; MIT's
  disclaimer applies and no quality/stability guarantee is made.
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

Not released; superseded by the `0.2.0` first release.

[0.2.0]: https://github.com/Yubickari/TouchKeyboard/releases/tag/v0.2.0
[0.1.0-preview.1]: https://github.com/Yubickari/TouchKeyboard/releases/tag/v0.1.0-preview.1
