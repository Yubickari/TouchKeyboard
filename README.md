# TouchKeyboard

**English** · [简体中文](README.zh.md)

A touch-first, embedded software keyboard for small industrial touch screens.
The input logic is a pure .NET Standard 2.0 backend; Avalonia is the first
frontend, and WPF is planned.

> **Status: P3 Avalonia frontend implemented.** The core has the ten input
> profiles, immutable layouts, session and validation; Avalonia now has the
> overlay host, controller, custom editor surface, touch keys, built-in
> adapters, theme and zh-CN/en-US localizer. P4 hardening and P5 publication
> remain. Do not use this package in production yet.

## Why

- Small screens (8–10 inch) and industrial panels need large keys and a layout
  that does not depend on the operating system's on-screen keyboard.
- Text, integer, decimal, date and IP fields should expose only the keys they
  actually accept; a positive integer field should not show a sign or decimal
  point.
- The input rules must be testable without a UI so a second frontend can reuse
  them.

## Planned shape

| Package | Target | Purpose |
|---|---|---|
| `TouchKeyboard.Core` | `netstandard2.0` | Modes, profiles, layouts, session, validation |
| `TouchKeyboard.Avalonia` | `net8.0;net10.0` | Avalonia host, keys, adapters, theme |
| `TouchKeyboard.Wpf` | future | WPF frontend, same core |

Two keyboard modes:

1. **Full keyboard** — a modern-phone interaction model with the standard US
   ANSI key order and number row. No F keys, numpad or system keys.
2. **Numeric keypad** — a general numeric surface (switchable between a 6×4
   numpad and a phone-style 123 page) and profile-restricted numeric surfaces
   for integer, decimal, date and IP fields.

Closing the panel discards edits; only **Confirm**/Enter commits. The field
label and unit are shown above the keys.

![Full keyboard](docs/images/keyboard-full.png)

![Profile-restricted numeric keypad](docs/images/keyboard-numeric.png)

*Headless Skia renders from the 1024×700 test harness.*

## What works now

- `InputProfile`: text, password, ASCII token, signed/positive integer,
  signed/positive decimal, date with `/` or `.`, and IPv4.
- Immutable `KeyboardLayout` data: the `PhoneHybrid` full keyboard, the general
  `Numpad`/`Phone` numeric surfaces, and profile-restricted numeric keypads that
  omit sign, decimal point, separator or ABC when they are not valid.
- `KeyboardSession`: insert, backspace, clear, caret movement, per-digit
  step-up/down, sign toggle, sticky shift, mode switch, commit/cancel, max
  length and numeric range checks.
- Avalonia: `TouchKeyboardHost` overlay with scrim, title/unit/error row,
  auto-attaching controller, custom-drawn `TouchKeyboardEditor`, pointer-captured
  `TouchKey` with long-press repeat, `KeyboardLayoutView`, `IEditorSurface`
  injection, adapters for `TextBox`, `NumericUpDown` and `CalendarDatePicker`,
  a custom adapter registry, light/dark themes and zh-CN/en-US localizer.
- 78 core tests + 10 Avalonia headless tests per target (176 executions).

Not implemented yet: WPF, NuGet publication (P5), Chinese input, and the P4
hardening pass (real-device DPI/OSK verification, accessibility, packaging
polish).

## Repository layout

```text
src/TouchKeyboard.Core/        pure backend
src/TouchKeyboard.Avalonia/    Avalonia frontend
samples/TouchKeyboard.Demo/    demo application
tests/TouchKeyboard.Core.Tests/
tests/TouchKeyboard.Avalonia.Tests/
docs/design/decisions.md       accepted decisions
docs/images/                   rendered previews
build/                         preflight and smoke scripts
```

## Build

Requires the .NET 8 and .NET 10 SDKs.

```bash
dotnet restore TouchKeyboard.sln
dotnet build TouchKeyboard.sln -c Release
dotnet test tests/TouchKeyboard.Core.Tests/TouchKeyboard.Core.Tests.csproj -c Release
dotnet test tests/TouchKeyboard.Avalonia.Tests/TouchKeyboard.Avalonia.Tests.csproj -c Release
dotnet pack src/TouchKeyboard.Core/TouchKeyboard.Core.csproj -c Release -o artifacts
dotnet pack src/TouchKeyboard.Avalonia/TouchKeyboard.Avalonia.csproj -c Release -o artifacts
```

Run the demo:

```bash
dotnet run --project samples/TouchKeyboard.Demo
```

The demo can be started with:

```bash
dotnet run --project samples/TouchKeyboard.Demo
```

Restore uses the standard NuGet package cache. In sandboxed environments, set
`NUGET_PACKAGES` to a writable directory before restoring.

## Design

See [docs/design/decisions.md](docs/design/decisions.md) for the accepted
decisions. The detailed design plan is kept locally until the public docs are
rewritten.

## License

MIT. See [LICENSE](LICENSE).
