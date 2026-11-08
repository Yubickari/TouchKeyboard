# ADR 0004 — Managed pinyin IME as an optional language pack

- **Status:** accepted (2026-11-08)
- **Deciders:** TouchKeyboard contributors
- **Related:** `design/decisions.md` D12 (revised), D17, D18, D19; `design/2026-11-08-chinese-input-and-language-packs.md`; `architecture.md`; `profiles.md`; `avalonia.md`; `release.md`
- **Supersedes:** ADR 0003 (the v1 no-IME plan was never released; `0.2.0` is the first release)

## Context

`TouchKeyboard` is an embedded, touch-first keyboard for small industrial panels. The
v1 baseline deliberately shipped no IME: it avoided platform input methods, key
injection, candidate windows and native dependencies, and reserved only an empty IME
slot and a `KeyGlyph.Ime`.

The product now needs Chinese input for common text and vocabulary. The target
devices often have no usable system IME; they must not trigger the platform OSK, and
the same keyboard must behave consistently on Windows, Linux and macOS. At the same
time the caller should not carry a large integration burden: a host should be able to
opt in by installing one optional package and adding one configuration line.

Three routes were evaluated: a managed pinyin engine (A), a bridge to the platform
IME (B), and key injection into the target control (C). B and C reintroduce the
platform, focus, permission and OSK risks that v1 removed. A keeps the input logic
managed and testable; its uncertainty is dictionary licensing and quality, which can
be handled with a small licensed lexicon and optional text packs.

## Decision

1. **Route A only.** Ship a managed pinyin engine with a composition state machine
   and a frontend-drawn candidate bar. Do not implement system IME bridging or key
   injection in `0.2.0`; do not promise their APIs.
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
   not introduced in `0.2.0`: it would add package and wiring burden for a small
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
   change the panel height within a mode. It is not an overlay.
8. **Chinese scope.** `0.2.0` covers pinyin only: full pinyin, word/sentence
   segmentation and common initial-letter abbreviations. No Wubi, handwriting, fuzzy
   pinyin, cloud input, user learning or Traditional/Simplified conversion.
9. **Punctuation.** The language pack maps common punctuation to full-width forms
   through `IImeEngine.TryMapPunctuation`; technical symbols stay ASCII and the host
   can disable the mapping. The mapping is language-owned, not a Core branch.
10. **Release target.** Ship the feature in `0.2.0`, optionally after a
    `0.2.0-preview.1` field trial. `0.2.0` is the first release; do not publish a
    separate `0.1.0`.
11. **Licensing.** Code is MIT. The base lexicon uses Unihan character readings
    (Unicode License) and the jieba word list/frequencies (MIT, with `NOTICE`), with
    generated phrase pronunciations and a project-owned polyphone override table.
    Optional text packs may use CC-BY 4.0 (attribution). No package ships CC-BY-SA,
    GPL/AGPL, proprietary or unknown-provenance data. A short `NOTICE` lists the
    sources and licences; no separate audit process is required. MIT's warranty
    disclaimer applies, and no candidate quality, coverage, performance or stability
    guarantee is made.
12. **0.2.0 layout baseline.** Before the IME lands, the no-IME layout removes the
    commandless IME spacer and lets Space fill the slot (AnsiStyle 7.0 units,
    PhoneHybrid 3.5). When an IME key is present, Space returns to 5.5/2.5 and the
    slot holds `ime.toggle`; toggling 中/英 keeps the key and does not change the
    width.

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
- Dictionary licensing, data quality, CJK fonts and 1024×600 layout are release
  checks, but licensing is handled with a short `NOTICE`, not a separate process.
- The optional pinyin and extended packages must be version-aligned with Core and
  published with their own README, icon, symbols and `NOTICE`.
- ADR 0003 was never released as a `0.1.0` product; `0.2.0` is the first release and
  supersedes it with this optional feature.
- The `0.2.0` no-IME baseline expands Space over the former IME slot on both full
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
  smaller but adds a package and wiring step for every caller. Rejected for `0.2.0`;
  the generic IME UI belongs to the Avalonia framework support.
- **Host-owned composition.** Keeps `KeyboardSession` untouched but duplicates key
  routing per frontend and leaves `KeyboardState` without a single render source.
  Rejected in favour of Core ownership.
- **Build an all-in-one Chinese IME immediately.** Wubi, handwriting, fuzzy pinyin
  and cloud input are outside the current product scope. Deferred.

## Follow-up

The design is implemented in `0.2.0`. Remaining work:

- Run the three-platform real-device checklist (Windows/Linux/macOS touch, CJK
  fonts, OSK suppression, DPI, 1024×600, dictionary load/memory).
- Publish the four packages and verify installation from the public feed.

The following items were completed with the implementation:

- Core/Avalonia/pinyin/extended code, data sources, hashes and licences in `NOTICE`.
- `docs/ime.md`, architecture/Avalonia/profile/layout/release docs, READMEs,
  CHANGELOG, four-package preflight/smoke and AOT/trim checks.
- 129-entry golden set (top-1 97.7%, top-3 100%) and local load/query timings in
  [ime.md](../ime.md).
