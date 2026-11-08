# ADR 0003 — No IME or Chinese input in the initial design

- **Status:** accepted (2026-11-06); **superseded by [ADR 0004](0004-managed-pinyin-ime.md)** (2026-11-08)
- **Deciders:** TouchKeyboard contributors
- **Related:** [Decision log](decisions.md) D12; [Profiles](../profiles.md); [Architecture](../architecture.md)
- **Scope of the supersession:** ADR 0004 adds the optional managed pinyin IME. This
  ADR is retained as historical context; the no-IME spacer detail is superseded by
  D19 (Space expands over the former IME slot instead of rendering a commandless
  spacer).

## Context

The target devices are kiosk/industrial panels. Chinese input requires an IME with
composition state, candidate windows and platform integration; injecting keys into
other processes requires OS-specific hooks (X11 XTEST, `SendInput`, `CGEvent`,
IBus). Both would make the library platform-dependent, hard to test headlessly and
large to maintain. The initial profiles also validate ASCII/digit-oriented values, which
an IME would not change.

## Decision

The initial design supports English letters, digits and symbols edited directly in the session
buffer. It does not implement an IME, composition, candidate windows, key injection
or system-level input hooks. The IME slot is reserved as a commandless spacer on the
full keyboards in the original design; D19 supersedes that detail for the no-IME
layout by expanding Space over the slot. `KeyGlyph.Ime` is reserved for a
future input-method switch. The custom-drawn editor surface avoids the platform text
input (and therefore the system OSK) for these fields.

## Consequences

- `password` accepts printable ASCII only; `text` accepts any non-control text, but
  the built-in keyboard cannot produce CJK characters without the optional IME.
- Hosts that needed CJK input used their own `IEditorSurface` (and accepted
  the platform behaviour of that control). The optional managed pinyin
  IME in ADR 0004 covers the common case without a platform IME or key injection.
- The core stays deterministic and testable; no platform interop in `netstandard2.0`.
- The IME slot remains an extension point for a later language switch; in the
  no-IME state it is folded into Space (AnsiStyle 7.0, PhoneHybrid 3.5) rather than
  rendered as a commandless spacer. `KeyGlyph.Ime` is still unused.
- Documentation must state the limitation prominently (README, profiles docs).

## Alternatives considered

- **System IME integration**: platform-specific, needs composition handling and
  candidate UI; incompatible with the headless-first architecture and the kiosk
  requirement to suppress the OSK.
- **A built-in pinyin/五笔 engine**: rejected for the initial design as too large
  for the baseline. ADR 0004 later reconsiders the managed pinyin part as an
  optional package; 五笔 remains out of scope.
- **Key injection into the target control**: would let the platform IME work, but
  requires OS-specific APIs, focus tricks and security-sensitive code. ADR 0004 keeps
  this out of the default path and does not plan an injection package.
