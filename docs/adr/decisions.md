# Decision log

This log records the decisions that shape the public API, the package layout
and the interaction model. Architecture decisions with alternatives and
consequences are recorded separately in ADR 0001–0004.

## Context

TouchKeyboard is an embedded, touch-first software keyboard for small
industrial screens. It must not depend on the operating system's on-screen
keyboard, must keep the input logic testable without a UI, and must allow a
later WPF frontend to reuse the same core.

Avalonia 12 ships assemblies for `net8.0` and `net10.0`, so the core and the
Avalonia frontend cannot share one target framework. The core is the
compatibility floor; each frontend targets the frameworks its UI framework
supports.

## Decisions

| # | Decision | Value |
|---|---|---|
| D1 | Full-keyboard geometry | `FullKeyboardLayoutKind.AnsiStyle` is the default: five rows of exactly 15 units with Tab/Caps reserved spaces and the ANSI Enter/Shift/backslash widths. `PhoneHybrid` is the retained four-row phone-style alternative with a full-keyboard-owned symbol page. |
| D2 | Numeric keypad | `NumericKeypadLayoutKind.Numpad` is the default and the only enabled surface: a 5×4 cell grid with a left function column, a 3×4 digit block, a right action column and a 1-unit Confirm. `Sequence` (5×6) is retained for custom scenarios. Profile-restricted numeric keypads use the Numpad and never enter the symbol page. |
| D3 | Panel editor surface | `TouchKeyboardEditor` (custom-drawn, not a `TextBox`) is the default. `IEditorSurface` lets a host inject its own editor control. |
| D4 | Package registry | NuGet.org, with a GitHub Release carrying the `.nupkg` and `.snupkg` artifacts. |
| D5 | Avalonia support | Avalonia 12 only; the adapter targets `net8.0;net10.0`. |
| D6 | Core target framework | `netstandard2.0` only, with no package dependencies. |
| D7 | License | Code is MIT. Optional language/text data uses only permissive or attribution licences (CC0, MIT, BSD, Apache-2.0, Unicode License, CC-BY 4.0) with a short `NOTICE`; no CC-BY-SA, GPL/AGPL, proprietary or unknown-provenance data. MIT's disclaimer applies; no input quality or stability guarantee is made. |
| D8 | Release artifacts | Each package ships its README, icon, XML documentation and symbol package; language packs also ship `NOTICE` and their dictionary resource. |
| D9 | Input profiles | `text`, `password`, `ascii-token`, `integer`, `positive-integer`, `decimal`, `positive-decimal`, `date-slash`, `date-dot`, `ip-address`. |
| D10 | Localization | Built-in `zh-CN` and `en-US`; core validation returns culture-neutral codes, not sentences. |
| D11 | Key height on small screens | 72 px default; 64 px below 720 px window height; 56 px below 640 px. |
| D12 | Chinese input | A managed pinyin IME is an optional feature: Core owns the composition state, `TouchKeyboard.Avalonia` carries the language-neutral IME UI, and `TouchKeyboard.Ime.Pinyin`/`.Extended` are optional packs. No system IME bridge or key injection. See ADR 0004. |
| D13 | Step-up/down semantics | Change the single digit immediately left of the caret, wrapping 0–9; the caret does not move. |
| D14 | Copyright holder | `TouchKeyboard contributors`. |
| D15 | Theme following | The overlay theme is automatic by default: it resolves the light or dark preset from the host window's actual theme variant and re-applies when it changes. An explicit `TouchKeyboardOptions.Theme` pins a preset. |
| D16 | Touch feedback | Keys and header chrome are not buttons and have no hover state. A press paints a theme overlay and scales the control down so feedback stays visible on a touch panel. |
| D17 | IME package layering | The base `TouchKeyboard.Avalonia` package carries generic IME UI and never references a language pack. Language data ships as optional `TouchKeyboard.Ime.*` packages; the common and extended pinyin packs reuse the same managed engine. Core keeps the composition contract and state machine, with no UI or platform dependency. |
| D18 | IME activation and gate | Activation is explicit and one-line: `options.UseIme(new PinyinImeEngine())`; no reflection, assembly scanning or DI requirement. No engine means no IME key or candidate row, and the no-IME layout uses expanded Space. `InputProfile.AllowsIme` defaults to `false` and is `true` only for `text`; a custom profile may opt in. |
| D19 | No-IME Space expansion | When no IME key is present, the full keyboards remove the commandless IME spacer and let Space fill it: AnsiStyle 7.0 units, PhoneHybrid 3.5. When an IME key is present, Space returns to 5.5/2.5 and the slot holds `ime.toggle`. Toggling 中/英 keeps the IME key, so the width does not change. Row totals stay 15/10. |

## Consequences

- `TouchKeyboard.Core` must not reference Avalonia, WPF, platform APIs, timers or
  dispatchers. An architecture test enforces this.
- `TouchKeyboard.Avalonia` targets `net8.0` and `net10.0`; a future
  `TouchKeyboard.Wpf` will target `net8.0-windows` and `net10.0-windows`.
- English, digits and symbols are edited directly in the session buffer. There
  is no X11 XTEST, `SendInput`, `CGEvent` or IBus integration; the optional IME
  keeps that boundary.
- Closing the panel always discards; only Submit/Enter commits.
- The default panel editor is custom-drawn, which avoids the system on-screen
  keyboard and platform IME composition. A host that needs its own editor
  control injects `IEditorSurface` and accepts the platform behaviour of that
  control.
- Numeric keypads are generated from `NumpadSlots`, derived from `InputProfile`
  and `NumericKeypadOptions`; irrelevant keys are absent rather than disabled.
  An empty slot changes the geometry: `0` widens over an empty decimal slot and
  Confirm stretches over an empty right-row-2 slot.
- The Numpad is a real 5×4 cell grid (`KeyboardGridLayout` with
  `ColumnSpan`/`RowSpan`), so spans are data, not per-row rendering tricks.
- Function keys carry a `KeyGlyph`; frontends draw their own vectors and keep
  the localized label as the accessible name and text fallback. A text key such
  as the IME toggle can provide a separate accessible label. The core never
  ships icon paths.
- `FullKeyboardPage` (`Main`/`Symbols`) is session state. The symbol page belongs
  to the full keyboard only; `AnsiStyle` keeps its symbols on the main page and
  Shift layer.
- The overlay follows the host window's theme variant by default. Keys and
  header chrome have no hover state; touch feedback is a themed overlay plus a
  small press scale.
- The optional IME adds a Core composition state machine, a language-neutral
  Avalonia candidate row and optional language packs. Data packs use
  permissive/attribution licences with a short `NOTICE`.
- IME API changes are additive (constructor overloads, methods, properties and
  the `KeyCommandKind.ToggleIme` value); existing callers keep compiling.
- No-IME full keyboards do not ship an inert IME spacer: Space expands over that
  slot, and the slot becomes `ime.toggle` only when an engine and an
  IME-capable profile are present.
- MIT's warranty disclaimer applies; documentation must not promise candidate
  quality, coverage, performance or stability beyond best-effort targets.
