# ADR 0003 — No IME or Chinese input in v1

- **Status:** accepted (2026-11-06); **superseded by [ADR 0004](0004-managed-pinyin-ime.md)** (2026-11-08); the v1 plan was never released as `0.1.0`
- **Deciders:** TouchKeyboard contributors
- **Related:** `design/decisions.md` D12; `profiles.md`; `architecture.md`
- **Scope of the supersession:** ADR 0004 adds an optional managed pinyin IME in
  `0.2.0`, which is the first release. The v1 decision below remains historical
  context; the no-IME spacer detail is superseded by D19 (`0.2.0` expands Space over
  the former IME slot instead of rendering a commandless spacer).

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
full keyboards in the original v1 design; D19 supersedes that detail for the `0.2.0`
no-IME layout by expanding Space over the slot. `KeyGlyph.Ime` is reserved for a
future input-method switch. The custom-drawn editor surface avoids the platform text
input (and therefore the system OSK) for these fields.

## Consequences

- `password` accepts printable ASCII only; `text` accepts any non-control text, but
  the built-in keyboard cannot produce CJK characters before `0.2.0`.
- Hosts that needed CJK before `0.2.0` used their own `IEditorSurface` (and accepted
  the platform behaviour of that control). From `0.2.0`, the optional managed pinyin
  IME in ADR 0004 covers the common case without a platform IME or key injection.
- The core stays deterministic and testable; no platform interop in `netstandard2.0`.
- The IME slot remains an extension point for a later language switch; in the `0.2.0`
  no-IME state it is folded into Space (AnsiStyle 7.0, PhoneHybrid 3.5) rather than
  rendered as a commandless spacer. `KeyGlyph.Ime` is still unused.
- Documentation must state the limitation prominently (README, profiles docs).

## Alternatives considered

- **System IME integration**: platform-specific, needs composition handling and
  candidate UI; incompatible with the headless-first architecture and the kiosk
  requirement to suppress the OSK.
- **A built-in pinyin/五笔 engine**: rejected for v1 as too large for the baseline.
  ADR 0004 later reconsiders the managed pinyin part as an optional `0.2.0` package;
  五笔 remains out of scope.
- **Key injection into the target control**: would let the platform IME work, but
  requires OS-specific APIs, focus tricks and security-sensitive code. ADR 0004 keeps
  this out of the default path and does not plan an injection package.
