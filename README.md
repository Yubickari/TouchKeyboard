# TouchKeyboard

**English** · [简体中文](README.zh.md)

A touch-first, embedded software keyboard for small industrial touch screens.
The input logic is a pure .NET Standard 2.0 backend; Avalonia is the first
frontend, and WPF is planned.

> **Status: P2 core implemented.** The .NET Standard core now has the ten input
> profiles, immutable layouts, the session state machine and validation, with
> 78 tests per target. The Avalonia panel/controller are still P1 placeholders.
> Do not use this package in production yet.

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

## What works now

- `InputProfile`: text, password, ASCII token, signed/positive integer,
  signed/positive decimal, date with `/` or `.`, and IPv4.
- Immutable `KeyboardLayout` data: the `PhoneHybrid` full keyboard, the general
  `Numpad`/`Phone` numeric surfaces, and profile-restricted numeric keypads that
  omit sign, decimal point, separator or ABC when they are not valid.
- `KeyboardSession`: insert, backspace, clear, caret movement, per-digit
  step-up/down, sign toggle, sticky shift, mode switch, commit/cancel, max
  length and numeric range checks.
- 78 core tests per target (net8.0 and net10.0).

Not implemented yet: the Avalonia panel/controller/editor surface (P3), WPF,
NuGet publication (P5), and Chinese input.

## Repository layout

```text
src/TouchKeyboard.Core/        pure backend
src/TouchKeyboard.Avalonia/    Avalonia frontend
samples/TouchKeyboard.Demo/    demo application
tests/TouchKeyboard.Core.Tests/
docs/design/decisions.md       accepted decisions
build/                         preflight and smoke scripts
```

## Build

Requires the .NET 8 and .NET 10 SDKs.

```bash
dotnet restore TouchKeyboard.sln
dotnet build TouchKeyboard.sln -c Release
dotnet test tests/TouchKeyboard.Core.Tests/TouchKeyboard.Core.Tests.csproj -c Release
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
