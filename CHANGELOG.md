# Changelog

All notable changes to this project are documented in this file.
The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [0.2.0] - 2026-11-08

First public release.

### Added

- `TouchKeyboard.Core`: ten built-in input profiles (`text`, `password`,
  `ascii-token`, `integer`, `positive-integer`, `decimal`, `positive-decimal`,
  `date-slash`, `date-dot`, `ip-address`), a case-insensitive profile registry,
  culture-neutral validation errors and commit normalization.
- Immutable keyboard layouts: the strict 15-unit `AnsiStyle` full keyboard, the
  four-row `PhoneHybrid` full keyboard with a symbol page, and the 5×4 `Numpad`
  cell grid with column/row spans, `NumpadSlots` scenario mapping and the
  retained `Sequence` numeric surface.
- `KeyboardSession` and immutable `KeyboardState` snapshots: insert, backspace,
  clear, caret movement, per-digit step, sign toggle, sticky Shift, mode/page
  switching, max length and numeric range checks, commit and cancel.
- Semantic key model: `KeyDefinition`, `KeyLabel`, `KeyStyle`, `KeyGlyph` and
  `KeyCommand`, so the core names icons and actions while a frontend draws them.
- Optional managed IME contracts in Core: `IImeEngine`, `ImeCandidate`,
  `ImeCompositionState`, composition routing, candidate paging and punctuation
  mapping. `InputProfile.AllowsIme` gates the feature; only `text` enables it by
  default.
- `TouchKeyboard.Avalonia`: in-window overlay host with scrim, title/unit/error
  row, custom-drawn single-line editor, variable-height keyboard area and fixed
  candidate row.
- Avalonia interaction: auto-attaching controller, pointer-captured touch keys,
  long-press repeat, physical keyboard handling, focus restore and OSK
  suppression through the custom editor surface.
- Avalonia layouts and rendering: rows and cell-grid rendering, vector
  `KeyGlyph` icons, automatic light/dark theme following, touch-first chrome and
  localized accessible names.
- Avalonia integration seams: `IEditorAdapter` with adapters for `TextBox`,
  `NumericUpDown` and `CalendarDatePicker`, a custom adapter registry,
  `IEditorSurface` injection and a localizer interface with built-in `zh-CN` and
  `en-US` strings.
- Appearance API: `TouchKeyboardOptions.Style` for overall/font/icon scale and
  semantic colours, full `TouchKeyboardTheme` overrides, and long-press
  title-row panel dragging with `ResetPanelPosition()`.
- Optional language packages: `TouchKeyboard.Ime.Pinyin` with a common lexicon
  and `TouchKeyboard.Ime.Pinyin.Extended` with a larger lexicon, both managed
  and dictionary-based.
- Avalonia IME UI: 中/英 key, candidate row, candidate paging, inline
  composition with underline, IME hints and full-width punctuation routing.
- Demo application with fields for every built-in profile, the optional pinyin
  engine, layout/theme selectors and appearance controls.
- Documentation set: architecture, Avalonia integration, input profiles,
  layout reference, IME, customization and public API.

### Notes

- The editor is single-line. Selection, clipboard, undo/redo, multi-line layout,
  F-keys, a NumLock keypad block, Ctrl/Alt/Win combinations and system keys are
  not provided.
- The optional pinyin engine does not bridge the system IME or inject keys, and
  does not provide Wubi, handwriting, fuzzy pinyin, cloud input, user learning
  or Traditional/Simplified conversion.
- Candidate quality and speed are best-effort; see
  [docs/ime.md](docs/ime.md) for scope and data sources.
- WPF is planned but not included in this release.

[Unreleased]: https://github.com/Yubickari/TouchKeyboard/compare/v0.2.0...HEAD
[0.2.0]: https://github.com/Yubickari/TouchKeyboard/releases/tag/v0.2.0
