# ADR 0003 — No IME or Chinese input in v1

- **Status:** accepted (2026-11-06)
- **Deciders:** TouchKeyboard contributors
- **Related:** `design/decisions.md` D12; `profiles.md`; `architecture.md`

## Context

The target devices are kiosk/industrial panels. Chinese input requires an IME with
composition state, candidate windows and platform integration; injecting keys into
other processes requires OS-specific hooks (X11 XTEST, `SendInput`, `CGEvent`,
IBus). Both would make the library platform-dependent, hard to test headlessly and
large to maintain. The v1 profiles also validate ASCII/digit-oriented values, which
an IME would not change.

## Decision

v1 supports English letters, digits and symbols edited directly in the session
buffer. It does not implement an IME, composition, candidate windows, key injection
or system-level input hooks. The IME slot is reserved as a commandless spacer on the
full keyboards, and `KeyGlyph.Ime` is reserved for a future input-method switch. The
custom-drawn editor surface avoids the platform text input (and therefore the
system OSK) for these fields.

## Consequences

- `text` and `password` profiles accept printable ASCII only; CJK text cannot be
  entered through the library's own keyboard.
- Hosts that need CJK use their own `IEditorSurface` (and accept the platform
  behaviour of that control) or wait for an optional injection/IME package.
- The core stays deterministic and testable; no platform interop in `netstandard2.0`.
- The IME spacer and `KeyGlyph.Ime` create a stable extension point for a later
  language switch without breaking layouts.
- Documentation must state the limitation prominently (README, profiles docs).

## Alternatives considered

- **System IME integration**: platform-specific, needs composition handling and
  candidate UI; incompatible with the headless-first architecture and the kiosk
  requirement to suppress the OSK.
- **A built-in pinyin/五笔 engine**: far outside the library's scope and would ship
  a language model.
- **Key injection into the target control**: would let the platform IME work, but
  requires OS-specific APIs, focus tricks and security-sensitive code; deferred to a
  possible optional package.
