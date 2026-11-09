# ADR 0004 — Managed pinyin IME as an optional language pack

- **Status:** accepted (2026-11-08)
- **Deciders:** TouchKeyboard contributors
- **Related:** [Decision log](decisions.md) D12 (revised), D17, D18, D19; [Architecture](../architecture.md); [Profiles](../profiles.md); [Avalonia integration](../avalonia.md); [Release runbook](../release.md)
- **Supersedes:** [ADR 0003](0003-no-ime-v1.md)

## Context

`TouchKeyboard` is an embedded, touch-first keyboard for small industrial panels. The
initial baseline deliberately shipped no IME: it avoided platform input methods, key
injection, candidate windows and native dependencies, and reserved only an empty IME
slot and a `KeyGlyph.Ime`.

The product now needs Chinese input for common text and vocabulary. The target
devices often have no usable system IME; they must not trigger the platform OSK, and
the same keyboard must behave consistently on Windows, Linux and macOS. At the same
time the caller should not carry a large integration burden: a host should be able to
opt in by installing one optional package and adding one configuration line.

Three routes were evaluated: a managed pinyin engine (A), a bridge to the platform
IME (B), and key injection into the target control (C). B and C reintroduce the
platform, focus, permission and OSK risks that the initial design removed. A keeps the input logic
managed and testable; its uncertainty is dictionary licensing and quality, which can
be handled with a small licensed lexicon and optional text packs.

## Decision

1. **Route A only.** Ship a managed pinyin engine with a composition state machine
   and a frontend-drawn candidate bar. Do not implement system IME bridging or key
   injection; do not promise their APIs.
2. **Core owns the composition.** `KeyboardSession` owns the IME composition state
   and exposes an immutable `ImeCompositionState` through `KeyboardState.Ime`.
   `KeyboardSession.Execute` routes IME keys; `ToggleIme` is a session command, not a
   host action. Core remains `netstandard2.0`, UI-free and platform-free.
3. **Avalonia owns the generic IME UI.** `TouchKeyboard.Avalonia` provides the IME
   key, the fixed candidate row, inline composition rendering, theming, localization
   and accessibility. It contains no language data and references no language pack.
4. **Language data ships as optional packages.** `TouchKeyboard.Ime.Pinyin` contains
   the pinyin engine and the common lexicon. `TouchKeyboard.Ime.Pinyin.Extended`
   reuses the common engine and adds a larger/domain lexicon. Future languages follow
   the same `IImeEngine` contract. A separate `TouchKeyboard.Avalonia.Ime` package is
   not introduced: it would add package and wiring burden for a small
   amount of generic UI code.
5. **One-line activation.** The host activates the feature with
   `options.UseIme(new PinyinImeEngine())`. Engine registration is explicit; there is
   no assembly scanning, reflection discovery, DI requirement or language-market UI.
   With no engine configured, no IME key or candidate row appears, and the no-IME
   layout uses expanded Space.
6. **Profile gate.** `InputProfile.AllowsIme` is `false` by default and `true` only
   for the `text` profile; password, ASCII token, integer, decimal, date and IPv4
   profiles stay IME-free. A custom profile may opt in, but the profile must also
   allow the full keyboard: a numeric-only profile has no letter surface to compose.
7. **Candidate row.** The candidate bar is a reserved row between the editor and the
   keyboard, 44 px by default on AnsiStyle. On PhoneHybrid it is the first of the
   five rows used by the 26-key main page and the general numeric page, so its height
   equals the key height and the main page matches the five-row symbol page; the
   symbol page hides it because it has no IME slot. Numeric-only profiles use a
   compact four-row keyboard area. Composition, page changes and 中/英 toggles never
   change the panel height within a mode. It is not an overlay. The IME key
   legend follows `KeyboardLanguage` (中/英 or CN/EN), but the slot width does not.
8. **Chinese scope.** The initial release covers pinyin only: full pinyin, word/sentence
   segmentation and common initial-letter abbreviations. No Wubi, handwriting, fuzzy
   pinyin, cloud input, user learning or Traditional/Simplified conversion.
9. **Punctuation.** The language pack maps common punctuation to full-width forms
   through `IImeEngine.TryMapPunctuation`; technical symbols stay ASCII and the host
   can disable the mapping. The mapping is language-owned, not a Core branch.
10. **Licensing.** Code is MIT. The base lexicon uses Unihan character readings
    (Unicode License) and the jieba word list/frequencies (MIT, with `NOTICE`), with
    generated phrase pronunciations and a project-owned polyphone override table.
    Optional text packs may use CC-BY 4.0 (attribution). No package ships CC-BY-SA,
    GPL/AGPL, proprietary or unknown-provenance data. A short `NOTICE` lists the
    sources and licences; no separate audit process is required. MIT's warranty
    disclaimer applies, and no candidate quality, coverage, performance or stability
    guarantee is made.
11. **No-IME layout baseline.** The no-IME layout removes the commandless IME spacer
    and lets Space fill the slot (AnsiStyle 7.0 units, PhoneHybrid 3.5). When an IME
    key is present, Space returns to 5.5/2.5 and the slot holds `ime.toggle`;
    toggling the IME mode (中/英 or CN/EN) keeps the key and does not change the width.

## Consequences

- The Core package gains a small UI-agnostic IME contract and state machine; the
  Avalonia package gains generic IME UI code (estimated 25–40 KB) that stays dormant
  without an engine.
- The public API is additive: `KeyboardState` gets a new constructor overload and an
  `Ime` property, `KeyboardLayouts.Full` gets an overload, `KeyDefinition` gets an
  optional `AccessibleName`, and `KeyCommandKind` gets `ToggleIme`. Existing callers
  keep compiling and running.
- `text` fields get Chinese input when the pinyin pack is installed; numeric, date,
  password and ASCII fields are unchanged.
- The composition state is shared by every frontend; a future WPF frontend only has
  to render `KeyboardState.Ime`.
- Dictionary licensing is recorded in a short `NOTICE`; data quality, CJK fonts and
  1024×600 layout are validated on target devices. The Avalonia host selects a
  platform-aware font stack and exposes `Style.FontFamily`; the packages do not
  bundle font files.
- The optional pinyin and extended packages are version-aligned with Core and
  published with their own README, icon, symbols and `NOTICE`.
- ADR 0003 is superseded by this optional feature.
- The no-IME baseline expands Space over the former IME slot on both full
  keyboards; the IME toggle is restored only when an engine and an IME-capable
  profile are present.
- The project remains free for anyone to use, modify and redistribute, with no
  commercial restriction. Documentation and package READMEs must not imply a quality
  or stability guarantee.

## Alternatives considered

- **System IME bridge (B).** Better input quality where a desktop IME already exists,
  but candidate UI is system-drawn, behavior varies by backend, and it can bring back
  the OSK. Not suitable as the industrial default; left as a possible future adapter
  for hosts that explicitly need it.
- **Key injection (C).** Reuses the host IME but needs three native implementations,
  permissions and focus tricks, and conflicts with the cross-platform, testable-core
  goal. Rejected for this library.
- **One mega-package with all languages.** Simplest install but violates “only install
  what you need” and maximizes size. Rejected.
- **Separate `TouchKeyboard.Avalonia.Ime` UI package (P2).** Keeps the base package
  smaller but adds a package and wiring step for every caller. Rejected; the generic
  IME UI belongs to the Avalonia framework support.
- **Host-owned composition.** Keeps `KeyboardSession` untouched but duplicates key
  routing per frontend and leaves `KeyboardState` without a single render source.
  Rejected in favour of Core ownership.
- **Build an all-in-one Chinese IME immediately.** Wubi, handwriting, fuzzy pinyin
  and cloud input are outside the current product scope. Deferred.
