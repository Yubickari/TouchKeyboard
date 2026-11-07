# TouchKeyboard Decision Record

**Status:** accepted · 2026-11-06 · revised 2026-11-07
**Scope:** `v0.1.0-preview.1`
**Supersedes:** the temporary design note kept outside this repository.
**Revision:** D1 and D2 were replaced by the full-keyboard and Numpad redesign;
see `docs/layouts.md` for the resulting geometry.

This record fixes the decisions that shape the public API and the package layout. It is intentionally shorter than the design plan; layout and interaction details live in `docs/layouts.md`; `docs/architecture.md` follows when the public docs are rewritten.

## Context

`TouchKeyboard` is an embedded, touch-first software keyboard for small industrial screens. It must not depend on the operating system's on-screen keyboard, must keep the input logic testable without a UI, and must allow a later WPF frontend to reuse the same core.

Avalonia 12 ships only `net8.0` and `net10.0` assemblies, so the core and the Avalonia frontend cannot share one target framework. The core is the compatibility floor; each frontend targets the frameworks its UI framework supports.

## Decisions

| # | Decision | Value |
|---|---|---|
| D1 | Full-keyboard geometry | `FullKeyboardLayoutKind.AnsiStyle` is the default: five rows of exactly 15 units with Tab/Caps/IME spacer cells and the exact Enter/Shift/backslash widths. `PhoneHybrid` is the retained alternative: four phone-style letter rows, no number row, and a full-keyboard-owned symbol page. |
| D2 | Numeric keypad | `NumericKeypadLayoutKind.Numpad` is the default and the only enabled surface: a 5×4 cell grid with a left function column, a 3×4 digit block, a right action column and a 1-unit Confirm. `Sequence` (renamed from `Phone`, 5×6) is retained for a future scenario but no built-in profile enables it. Profile-restricted numeric keypads use the Numpad and never enter the symbol page. |
| D3 | Panel editor surface | `TouchKeyboardEditor` (custom-drawn, not a `TextBox`) is the default. `IEditorSurface` lets a host inject its own real editor control. |
| D4 | Package registry | NuGet.org, with a GitHub Release carrying `.nupkg` and `.snupkg` artifacts. |
| D5 | Avalonia support | Avalonia 12 only; the adapter targets `net8.0;net10.0`. |
| D6 | Core target framework | `netstandard2.0` only, with no package dependencies. |
| D7 | License | MIT. |
| D8 | Repository visibility | Private until the preview is ready, then public. |
| D9 | v1 input profiles | `Text`, `Password`, `AsciiToken`, `Integer`, `PositiveInteger`, `Decimal`, `PositiveDecimal`, `DateSlash`, `DateDot`, `IpAddress`. |
| D10 | Localization | Built-in `zh-CN` and `en-US`; core returns error codes, not sentences. |
| D11 | Key height on 600 px screens | 64 px; 56 px below 600 px. |
| D12 | Chinese input | Out of scope for v1. No IME, no key injection; an optional injection package may come later. |
| D13 | Step-up/down semantics | Change the single digit immediately left of the caret, wrapping 0–9; the caret does not move. |
| D14 | Copyright holder | `TouchKeyboard contributors`. |
| D15 | Theme following | The overlay theme is automatic by default: it resolves the light or dark preset from the host window's actual theme variant and re-applies when it changes. An explicit `TouchKeyboardOptions.Theme` pins a preset. |
| D16 | Touch feedback | Keys and header chrome are not buttons and have no hover state. A press paints a theme overlay and scales the control down, so feedback stays visible on a touch panel. |

## Consequences

- `TouchKeyboard.Core` must not reference Avalonia, WPF, platform APIs, timers or dispatchers. An architecture test enforces this.
- `TouchKeyboard.Avalonia` targets `net8.0` and `net10.0`; `TouchKeyboard.Wpf` will target `net8.0-windows` and `net10.0-windows` later.
- English/digits/symbols are edited directly in the session buffer. There is no X11 XTEST, `SendInput`, `CGEvent` or IBus integration in v1.
- Closing the panel always discards; only Submit/Enter commits.
- The panel editor is custom-drawn by default, which avoids the system on-screen keyboard and IME composition state. A host that needs its own editor control injects `IEditorSurface` and accepts the platform behavior of that control.
- Numeric keypads are generated from `NumpadSlots`, derived from `InputProfile` + `NumericKeypadOptions`; irrelevant keys are absent rather than disabled. An empty slot changes the geometry instead of rendering a dead key: `0` widens over an empty decimal slot and Confirm stretches over an empty right-row2 slot.
- The Numpad is a real 5×4 cell grid (`KeyboardGridLayout` with `ColumnSpan`/`RowSpan`), so spans are data, not per-row rendering tricks.
- Function keys carry a `KeyGlyph`; frontends draw their own vectors and keep the localized label as the accessible name and text fallback. The core never ships icon paths.
- `FullKeyboardPage` (`Main`/`Symbols`) is session state. The symbol page belongs to the full keyboard only and only PhoneHybrid has one in v1.
- The overlay follows the host window's theme variant by default, so a dark shell gets the dark keyboard without host code. Keys and header chrome have no hover state; touch feedback is a themed overlay plus a small press scale.
- The release pipeline uses NuGet Trusted Publishing (OIDC), not a long-lived API key, mirroring the release discipline used by the maintainer's npm packages.

## Open details (not blocking the package skeleton)

1. **PhoneHybrid punctuation.** Resolved by the redesign: PhoneHybrid is letters-only on the main page and reaches punctuation through its owned 24-symbol page. AnsiStyle keeps full ANSI punctuation on the main page and Shift layer.
2. **AnsiStyle symbol page.** Not in v1; Shift already covers every ANSI symbol. `KeyboardLayouts.Full(AnsiStyle, Symbols)` falls back to the main page, and the session ignores the symbol-page command for that geometry. Revisit if a dedicated entry key is wanted.
3. **Symbol set.** The 24-symbol page is an initial common set and can be tuned as a data change. The retained `Sequence` page keeps its 5×6 set.
4. **Numpad right row-2 slot.** Reserved for scenario extras; no built-in scenario fills it in v1. `KeyboardLayouts.Numpad(id, slots)` and `NumpadSlotKey` are the extension point.
5. **DI extension.** `AddTouchKeyboard` may live in the Avalonia package or a thin optional hosting package; the core stays dependency-free.
6. **Icon style.** The caret-bar set (filled triangle plus a caret bar; triangle paired with a `+`/`−` sign) is the shipped look. `docs/images/icon-variants.png` keeps the two alternatives (shafted arrows and double chevrons) in case the style is revisited.
7. **Public repository timing.** Private until `v0.1.0-preview.1` passes the release preflight, then public.
