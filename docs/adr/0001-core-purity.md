# ADR 0001 — Keep the core free of UI and platform dependencies

- **Status:** accepted (2026-11-06)
- **Deciders:** TouchKeyboard contributors
- **Related:** `design/decisions.md` D3, D5, D6, D12

## Context

The keyboard should be reusable by more than one UI stack (Avalonia now, WPF later)
and its input logic must be testable without a display server. The reference
implementation mixed editing logic, rendering and platform focus handling in one
place, which made behaviour hard to verify and reuse.

## Decision

`TouchKeyboard.Core` targets `netstandard2.0` with no package dependencies and never
references a UI framework, platform API, timer or dispatcher. It owns the domain
model, profiles, validation, layouts, the editing session and immutable state
snapshots. Rendering, pointer capture, focus, long-press timing, fonts, brushes,
localization text and platform OSK behaviour belong to the frontend packages.

An architecture test fails the build if the core assembly references Avalonia, WPF,
`PresentationFramework`, `WindowsBase`, X11 or similar.

## Consequences

- The core can be unit-tested with plain `dotnet test`, including layout matrices and
  session behaviour.
- Frontends must implement drawing and input themselves; icon paths and theme
  presets are frontend assets, while the core only names `KeyGlyph` values.
- Timing-dependent behaviour (long-press repeat, caret blink) lives in the frontend,
  so the core state machine stays deterministic.
- A second frontend reuses the core without duplicating validation or session logic.
- Some frontend concepts (focus, accessibility peers) are deliberately not modelled
  in the core.

## Alternatives considered

- **A shared UI abstraction** (`IKeyView` etc.) in the core: rejected because it
  recreates a UI framework inside the library and still cannot unify WPF/Avalonia
  event models.
- **Logic inside the Avalonia package**: simplest for the first frontend, rejected
  because it blocks the WPF port and headless testing of input rules.
- **Multi-targeting the core to `net8.0` for modern APIs**: rejected for the first
  preview; the compatibility floor is more valuable than a few convenience APIs.
