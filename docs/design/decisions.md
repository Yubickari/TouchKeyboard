# TouchKeyboard Decision Record

**Status:** accepted · 2026-11-06
**Scope:** `v0.1.0-preview.1`
**Supersedes:** the temporary design note kept outside this repository.

This record fixes the decisions that shape the public API and the package layout. It is intentionally shorter than the design plan; layout and interaction details live in `docs/layouts.md` and `docs/architecture.md` once those are published.

## Context

`TouchKeyboard` is an embedded, touch-first software keyboard for small industrial screens. It must not depend on the operating system's on-screen keyboard, must keep the input logic testable without a UI, and must allow a later WPF frontend to reuse the same core.

Avalonia 12 ships only `net8.0` and `net10.0` assemblies, so the core and the Avalonia frontend cannot share one target framework. The core is the compatibility floor; each frontend targets the frameworks its UI framework supports.

## Decisions

| # | Decision | Value |
|---|---|---|
| D1 | Full-keyboard geometry | `FullKeyboardLayoutKind.PhoneHybrid` by default: modern phone ergonomics with the standard US ANSI key order and number row. `AnsiStrict` is reserved for a future strict physical-ANSI layout. |
| D2 | General numeric keypad | `NumericKeypadLayoutKind.Numpad` (6×4) and `.Phone` (phone-style 123 page, 5×6) are switchable at runtime; `Numpad` is the default. Profile-restricted numeric keypads do not switch. |
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

## Consequences

- `TouchKeyboard.Core` must not reference Avalonia, WPF, platform APIs, timers or dispatchers. An architecture test enforces this.
- `TouchKeyboard.Avalonia` targets `net8.0` and `net10.0`; `TouchKeyboard.Wpf` will target `net8.0-windows` and `net10.0-windows` later.
- English/digits/symbols are edited directly in the session buffer. There is no X11 XTEST, `SendInput`, `CGEvent` or IBus integration in v1.
- Closing the panel always discards; only Submit/Enter commits.
- The panel editor is custom-drawn by default, which avoids the system on-screen keyboard and IME composition state. A host that needs its own editor control injects `IEditorSurface` and accepts the platform behavior of that control.
- Numeric keypads are generated from `InputProfile` + `NumericKeypadOptions`; irrelevant keys are absent rather than disabled.
- The release pipeline uses NuGet Trusted Publishing (OIDC), not a long-lived API key, mirroring the release discipline used by the maintainer's npm packages.

## Open details (not blocking the package skeleton)

1. **PhoneHybrid punctuation.** The current assumption keeps ANSI punctuation (`[]\;',./`) on the letter rows. A more iOS/Gboard-like variant would move punctuation into the 123 page; that would be a data change, not an architectural change.
2. **AnsiStrict.** The enum value is reserved; v1 does not implement it.
3. **Phone 123 symbol set.** The 5×6 page currently carries digits plus `- / : ; ( ) $ & @ " . , ? !`. The set can be tuned without changing the core.
4. **DI extension.** `AddTouchKeyboard` may live in the Avalonia package or a thin optional hosting package; the core stays dependency-free.
5. **Public repository timing.** Private until `v0.1.0-preview.1` passes the release preflight, then public.
