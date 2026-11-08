# ADR 0002 — Target framework matrix

- **Status:** accepted (2026-11-06)
- **Deciders:** TouchKeyboard contributors
- **Related:** [Decision log](decisions.md) D5, D6; [Architecture](../architecture.md)

## Context

Avalonia 12 ships assemblies only for `net8.0` and `net10.0`. The core should be
consumable by the widest reasonable set of .NET applications and must not force a
particular UI stack's TFM on consumers. The package is also expected to gain a WPF
frontend later.

## Decision

| Project | Target frameworks |
|---|---|
| `TouchKeyboard.Core` | `netstandard2.0` |
| `TouchKeyboard.Avalonia` | `net8.0;net10.0` |
| `TouchKeyboard.Demo` | `net8.0` |
| Core and Avalonia test projects | `net8.0;net10.0` |
| Future `TouchKeyboard.Wpf` | `net8.0-windows;net10.0-windows` |

The solution builds and tests both Avalonia target frameworks on every CI run.

## Consequences

- The core is consumable from .NET Framework 4.6.1+, .NET Core 2.0+, .NET 5+ and
  Unity-style environments, as long as the consumer uses a supported TFM for the
  frontend.
- Core code must stay within the `netstandard2.0` API surface even though
  `LangVersion` is 12; newer BCL helpers need small compatibility shims.
- Avalonia 11 projects cannot use the package directly. That is documented rather
  than maintained as a second package.
- CI runs Ubuntu and Windows on every push. The macOS job is gated to public
  repositories (macOS runners are unavailable for private repositories on the
  current plan) and macOS is also verified locally before a release.

## Alternatives considered

- **Target the core at `net8.0` only**: simpler and allows modern APIs, but loses
  the wide compatibility floor for no functional gain in the initial release.
- **Multi-target the core (`netstandard2.0;net8.0`)**: possible later if a measurable
  need appears; adds build and test cost now.
- **Ship Avalonia 11 builds too**: requires maintaining two frontend packages and a
  second API baseline; the initial plan declined dual maintenance.
