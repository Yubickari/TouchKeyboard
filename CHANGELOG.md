# Changelog

**English** · [简体中文](CHANGELOG.zh.md)

All notable changes to this project are documented in this file.
The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [0.3.0] - 2026-11-09

PhoneHybrid symbol-page completion.

### Added

- The PhoneHybrid symbol page now carries the complete 32-symbol US ANSI set,
  including the previously missing backtick, double quote, `<`, `>`, comma,
  period, question mark and slash.
- A `123` numeric entry key right of `ABC` and a Clear key immediately left of
  Backspace on the symbol-page function row.
- When an IME engine and an IME-capable profile are active, the symbol-page Clear
  slot becomes the IME toggle (`中/英` or `CN/EN`); without an engine the slot
  remains Clear.
- In Chinese IME mode, mapped symbol caps follow the language (for example
  `.`→`。`, `,`→`，`, `?`→`？`, `!`→`！`, and bracket/brace pairs). Commands still
  insert the ASCII source and the session maps it; English mode and
  `FullWidthPunctuation = false` keep ASCII caps.

### Changed

- Rebuilt the symbol page as a 4×8 symbol grid plus a separate 8-key function
  row: `ABC 123 ← Space → Clear ⌫ Confirm`. All 40 cells are one unit wide.
- Backtick and tilde are the first two symbols; apostrophe, double quote, `<`,
  `>`, comma, period, `/` and `?` are the last eight, in that order.
- The symbol-page row total grows from 6 to 8 units so the symbol grid and the
  function row share one aligned width.

### Notes

- No public type or method was removed. The layout id `full-symbol-page`, the
  `FullKeyboardPage.Symbols` state and the existing key commands are unchanged.

## [0.2.1] - 2026-11-09

Maintenance and language/font release.

### Added

- `KeyboardLanguage` (`Auto`, `Chinese`, `English`) and
  `TouchKeyboardOptions.KeyLanguage`: switch key legends, chrome and validation
  messages without changing the US ANSI QWERTY geometry. `Auto` follows
  `Culture`, then the current UI culture; a custom `ITouchKeyboardLocalizer`
  still wins.
- Fully English key surface: `Confirm`, `Clear`, `Symbols`, `Space`, `Enter` and
  an IME toggle that reads `CN`/`EN` instead of 中/英. The English validation
  messages and accessible names are unchanged in meaning.
- `TouchKeyboardFonts` with per-platform default chains: Windows
  `Segoe UI, Microsoft YaHei UI, Microsoft YaHei`, macOS `Helvetica Neue,
  PingFang SC, Hiragino Sans GB`, Linux `Noto Sans, Noto Sans CJK SC,
  WenQuanYi Micro Hei, DejaVu Sans`.
- `TouchKeyboardTheme.FontFamily` and `TouchKeyboardStyle.FontFamily`. The host
  applies the family to key caps, the title/unit/error row, the editor surface,
  candidate numbers/text, candidate hints and paging glyphs.
- Chinese versions of the integration guide (`docs/avalonia.zh.md`), input
  profile reference (`docs/profiles.zh.md`), layout reference
  (`docs/layouts.zh.md`), demo README, package READMEs and changelog
  (`CHANGELOG.zh.md`).
- `AGENTS.md` and contributor rules that require a synchronized Chinese version
  of every user-facing document.

### Changed

- The default keyboard font is now platform-aware instead of always using the
  Avalonia default family. Font files are not bundled; a CJK face is still
  required on the machine.
- English localizer: the active IME label changed from 中 to `CN`, so an
  all-English host has no stray Chinese legend on the IME key.
- Avalonia packages build against the 12.0.5 compatibility floor and declare
  `Avalonia >= 12.0.5`; CI also tests the suite against 12.1.5.
- Updated `Microsoft.NET.Test.Sdk` from 17.12.0 to 18.11.0.
- Updated the GitHub Actions used by CI and release to
  `actions/checkout@v7`, `actions/setup-dotnet@v6` and
  `actions/upload-artifact@v7`.
- Documentation now records that basic real-device acceptance was completed on
  Windows, Linux and macOS in November 2026. Platform-specific details are
  tracked through GitHub issues and the device checklist.

### Notes

- Key geometry, row widths, IME slot widths and candidate-row heights are
  unchanged by `KeyLanguage`. Only text changes.
- `KeyboardLanguage.English` does not disable the optional pinyin engine by itself. An
  all-English host can omit `TouchKeyboard.Ime.Pinyin`/`UseIme(...)`, or keep the
  engine and expose an English legend and a CN/EN toggle.
- The GitHub release body is generated from the matching section of
  `CHANGELOG.zh.md`.

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

[Unreleased]: https://github.com/Yubickari/TouchKeyboard/compare/v0.3.0...HEAD
[0.3.0]: https://github.com/Yubickari/TouchKeyboard/releases/tag/v0.3.0
[0.2.1]: https://github.com/Yubickari/TouchKeyboard/releases/tag/v0.2.1
[0.2.0]: https://github.com/Yubickari/TouchKeyboard/releases/tag/v0.2.0
