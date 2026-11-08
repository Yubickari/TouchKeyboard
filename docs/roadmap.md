# Roadmap

This page records future directions, not delivery commitments. Feedback and
feature requests are handled through the GitHub issue templates.

## Planned

- **WPF frontend.** Reuse the core with a WPF host, touch keys, adapters and
  theme; see [WPF porting notes](contributing/wpf-porting.md).
- **More input profiles.** Time (`:` separator), numeric ranges, currency
  formatting at the host boundary, IPv6 and custom masks.
- **More numeric scenarios.** Fill the unused right-row-2 Numpad slot and
  enable the retained `Sequence` surface for serial-number entry.
- **More language packs.** A new `TouchKeyboard.Ime.<Language>` package can
  implement `IImeEngine` and reuse the Avalonia IME UI.
- **Frontend polish.** High-contrast theme, optional key sounds/haptics through
  host callbacks and screen-reader announcements for validation.
- **Tooling.** Formatting gates, coverage reporting, benchmarks and automated
  screenshot generation.

## Not planned

- System IME bridging, key injection or cross-process input.
- Wubi, handwriting, fuzzy pinyin, cloud input, self-learning or
  Traditional/Simplified conversion.
- A multi-language switching UI, language marketplace or dynamic plugin
  discovery.
- System-level keyboard hooks or a global OSK replacement.
- F1–F12, a NumLock keypad block, Ctrl/Alt/Win/Menu/PrintScreen keys or a
  system-key layer.
- Multi-line editing, selection, clipboard or undo/redo.
- Mobile/WebAssembly targets or iOS/Android gesture models.
- Business validation such as required-field rules, cross-field checks or
  domain workflows; the host's view model remains the source of truth.
