# TouchKeyboard Decision Record

**Status:** accepted · 2026-11-06 · revised 2026-11-08
**Scope:** `0.2.0` (first release; `0.1.0` is not released)
**Supersedes:** the temporary design note kept outside this repository.
**Revision:** D1 and D2 were replaced by the full-keyboard and Numpad redesign;
see `docs/layouts.md` for the resulting geometry. D12 was revised and D17/D18 were
added by the optional managed pinyin IME decision; D7 records the simple
MIT/permissive-data licence policy; D19 records the no-IME Space expansion inside
`0.2.0`. See `docs/design/2026-11-08-chinese-input-and-language-packs.md` and
`docs/adr/0004-managed-pinyin-ime.md`.

This record fixes the decisions that shape the public API and the package layout. It is intentionally shorter than the design plan; layout and interaction details live in `docs/layouts.md`; `docs/architecture.md` follows when the public docs are rewritten.

## Context

`TouchKeyboard` is an embedded, touch-first software keyboard for small industrial screens. It must not depend on the operating system's on-screen keyboard, must keep the input logic testable without a UI, and must allow a later WPF frontend to reuse the same core.

Avalonia 12 ships only `net8.0` and `net10.0` assemblies, so the core and the Avalonia frontend cannot share one target framework. The core is the compatibility floor; each frontend targets the frameworks its UI framework supports.

## Decisions

| # | Decision | Value |
|---|---|---|
| D1 | Full-keyboard geometry | `FullKeyboardLayoutKind.AnsiStyle` is the default: five rows of exactly 15 units with Tab/Caps reserved spaces and the exact Enter/Shift/backslash widths; the IME slot follows D19. `PhoneHybrid` is the retained alternative: four phone-style letter rows, no number row, and a full-keyboard-owned symbol page. |
| D2 | Numeric keypad | `NumericKeypadLayoutKind.Numpad` is the default and the only enabled surface: a 5×4 cell grid with a left function column, a 3×4 digit block, a right action column and a 1-unit Confirm. `Sequence` (renamed from `Phone`, 5×6) is retained for a future scenario but no built-in profile enables it. Profile-restricted numeric keypads use the Numpad and never enter the symbol page. |
| D3 | Panel editor surface | `TouchKeyboardEditor` (custom-drawn, not a `TextBox`) is the default. `IEditorSurface` lets a host inject its own real editor control. |
| D4 | Package registry | NuGet.org, with a GitHub Release carrying `.nupkg` and `.snupkg` artifacts. |
| D5 | Avalonia support | Avalonia 12 only; the adapter targets `net8.0;net10.0`. |
| D6 | Core target framework | `netstandard2.0` only, with no package dependencies. |
| D7 | License | Code is MIT. Optional language/text data uses only permissive or attribution licences (CC0, MIT, BSD, Apache-2.0, Unicode License, CC-BY 4.0) with a short `NOTICE`; no CC-BY-SA, GPL/AGPL, proprietary or unknown-provenance data. No separate custom warranty text: MIT's disclaimer applies, and no input quality or stability guarantee is made. |
| D8 | Repository visibility | Private until the `0.2.0` release preflight passes, then public. |
| D9 | v1 input profiles | `Text`, `Password`, `AsciiToken`, `Integer`, `PositiveInteger`, `Decimal`, `PositiveDecimal`, `DateSlash`, `DateDot`, `IpAddress`. |
| D10 | Localization | Built-in `zh-CN` and `en-US`; core returns error codes, not sentences. |
| D11 | Key height on 600 px screens | 72 px by default; 64 px below 720 px window height; 56 px below 640 px. |
| D12 | Chinese input | A managed pinyin IME is an optional `0.2.0` feature (route A only): Core owns the composition state, `TouchKeyboard.Avalonia` carries the language-neutral IME UI, and `TouchKeyboard.Ime.Pinyin`/`.Extended` are optional packs. `0.2.0` is the first release; no separate `0.1.0`. No system IME bridge or key injection. See ADR 0004. |
| D13 | Step-up/down semantics | Change the single digit immediately left of the caret, wrapping 0–9; the caret does not move. |
| D14 | Copyright holder | `TouchKeyboard contributors`. |
| D15 | Theme following | The overlay theme is automatic by default: it resolves the light or dark preset from the host window's actual theme variant and re-applies when it changes. An explicit `TouchKeyboardOptions.Theme` pins a preset. |
| D16 | Touch feedback | Keys and header chrome are not buttons and have no hover state. A press paints a theme overlay and scales the control down, so feedback stays visible on a touch panel. |
| D17 | IME package layering | The base `TouchKeyboard.Avalonia` package carries generic IME UI and never references a language pack. Language data ships as optional `TouchKeyboard.Ime.*` packages; the pinyin common and extended packs reuse the same managed engine. Core keeps the composition contract and state machine, with no UI or platform dependency. |
| D18 | IME activation and gate | Activation is explicit and one-line: `options.UseIme(new PinyinImeEngine())`; no reflection, assembly scanning or DI requirement. No engine means no IME key or candidate row, and the no-IME layout uses expanded Space. `InputProfile.AllowsIme` defaults to `false` and is `true` only for `text`; a custom profile may opt in. |
| D19 | No-IME Space expansion | When no IME key is present (`0.2.0` with no engine or a profile that disallows IME), the full keyboards remove the commandless IME spacer and let Space fill it: AnsiStyle 7.0 units, PhoneHybrid 3.5. When an IME key is present, Space returns to 5.5/2.5 and the slot holds `ime.toggle`. Toggling 中/英 keeps the IME key, so the width does not change. Row totals stay 15/10. |

## Consequences

- `TouchKeyboard.Core` must not reference Avalonia, WPF, platform APIs, timers or dispatchers. An architecture test enforces this.
- `TouchKeyboard.Avalonia` targets `net8.0` and `net10.0`; `TouchKeyboard.Wpf` will target `net8.0-windows` and `net10.0-windows` later.
- English/digits/symbols are edited directly in the session buffer. There is no X11 XTEST, `SendInput`, `CGEvent` or IBus integration in the default path; the optional `0.2.0` IME keeps it that way.
- Closing the panel always discards; only Submit/Enter commits.
- The panel editor is custom-drawn by default, which avoids the system on-screen keyboard and the platform IME composition state; the optional managed pinyin IME draws its own composition. A host that needs its own editor control injects `IEditorSurface` and accepts the platform behavior of that control.
- Numeric keypads are generated from `NumpadSlots`, derived from `InputProfile` + `NumericKeypadOptions`; irrelevant keys are absent rather than disabled. An empty slot changes the geometry instead of rendering a dead key: `0` widens over an empty decimal slot and Confirm stretches over an empty right-row2 slot.
- The Numpad is a real 5×4 cell grid (`KeyboardGridLayout` with `ColumnSpan`/`RowSpan`), so spans are data, not per-row rendering tricks.
- Function keys carry a `KeyGlyph`; frontends draw their own vectors and keep the localized label as the accessible name and text fallback. A text key such as the IME toggle can provide a separate accessible label. The core never ships icon paths.
- `FullKeyboardPage` (`Main`/`Symbols`) is session state. The symbol page belongs to the full keyboard only and only PhoneHybrid has one in v1.
- The overlay follows the host window's theme variant by default, so a dark shell gets the dark keyboard without host code. Keys and header chrome have no hover state; touch feedback is a themed overlay plus a small press scale.
- The release pipeline uses NuGet Trusted Publishing (OIDC), not a long-lived API key, mirroring the release discipline used by the maintainer's npm packages.
- The optional IME adds a Core composition state machine, a language-neutral Avalonia candidate row and optional language packs; `0.2.0` is the first release, so there is no earlier `0.1.0` baseline to preserve. Data packs use permissive/attribution licences with a short `NOTICE`; no separate audit process is required.
- IME API changes are additive (new constructor overloads, methods, properties and the `KeyCommandKind.ToggleIme` value); existing callers keep compiling and running. See the compatibility table in the design plan.
- The no-IME full keyboards no longer ship an inert IME spacer: Space expands over that slot in the `0.2.0` no-IME state, and the slot becomes `ime.toggle` only when an engine and an IME-capable profile are present.
- MIT's warranty disclaimer applies; documentation must not promise candidate quality, coverage, performance or stability beyond best-effort development targets.

## Open details (not blocking the package skeleton)

1. **PhoneHybrid punctuation.** Resolved by the redesign: PhoneHybrid is letters-only on the main page and reaches punctuation through its owned 24-symbol page. AnsiStyle keeps full ANSI punctuation on the main page and Shift layer.
2. **AnsiStyle symbol page.** Not in v1; Shift already covers every ANSI symbol. `KeyboardLayouts.Full(AnsiStyle, Symbols)` falls back to the main page, and the session ignores the symbol-page command for that geometry. Revisit if a dedicated entry key is wanted.
3. **Symbol set.** The 24-symbol page is an initial common set and can be tuned as a data change. The retained `Sequence` page keeps its 5×6 set.
4. **Numpad right row-2 slot.** Reserved for scenario extras; no built-in scenario fills it in v1. `KeyboardLayouts.Numpad(id, slots)` and `NumpadSlotKey` are the extension point.
5. **DI extension.** `AddTouchKeyboard` may live in the Avalonia package or a thin optional hosting package; the core stays dependency-free.
6. **Icon style.** The caret-bar set (filled triangle plus a caret bar; triangle paired with a `+`/`−` sign) is the shipped look. `docs/images/icon-variants.png` keeps the two alternatives (shafted arrows and double chevrons) in case the style is revisited.
7. **Public repository timing.** Private until the `0.2.0` release preflight passes, then public.

The IME design decisions are closed in D12/D17/D18/D19 and ADR 0004. Remaining IME
items in the design plan are implementation work and data-source/licence recording,
not open design questions.
