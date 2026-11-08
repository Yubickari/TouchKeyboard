# Roadmap

Status as of 2026-11-08. `0.2.0` is the first public release; implementation,
preflight/smoke and AOT/trim are complete. The remaining work is the real-device
run and publishing. `0.1.0` was never released.

## Current state

| Area | State |
|---|---|
| Core | Ready: profiles, `AnsiStyle`/`PhoneHybrid`, 5×4 `Numpad`, session, validation, IME composition. |
| Avalonia | Ready: host/controller, custom editor, IME key and candidate row, theming, localizer, adapters, panel drag. |
| Language packs | Ready: `TouchKeyboard.Ime.Pinyin` (~20k words) and `.Extended` (~100k words), both with `NOTICE`. |
| Packaging/docs | Ready: four-package preflight/smoke, AOT/trim, architecture/API/IME/customization docs and screenshots. |
| `0.2.0` release | Pending: three-platform real-device validation and publication. |

## Release readiness

- [x] Core, pinyin and headless test suites (135 core + 40 pinyin + 48 Avalonia per target).
- [x] Layout, architecture, Avalonia, profile, release, IME, customization, public-API and WPF docs.
- [x] ADRs for core purity, TFM matrix, the no-IME v1 decision and the managed pinyin IME.
- [x] Package icon, package-content checks, issue templates, release workflow.
- [x] Package smoke (Avalonia-only / +Pinyin / +Extended) and AOT/trim wired into CI.
- [x] No-IME layout baseline: Space 7.0/3.5, no inert spacer; IME key restores 5.5/2.5.
- [x] Optional managed pinyin IME, language packs, Avalonia IME UI and demo field.
- [x] Compact appearance API and long-press title-row panel dragging.
- [x] Language-pack `NOTICE` and source/licence records; no separate legal process.
- [ ] Three-platform real-device run: 8″ 1280×800 and 10″ 1024×600 Windows touch,
      Ubuntu X11/Wayland, macOS; DPI 100/125/150/200%. Use
      [testing/device-checklist.md](testing/device-checklist.md).
- [ ] Confirm system OSK suppression and physical-keyboard isolation on Windows.

## 0.2.0 release

1. Configure nuget.org **Trusted Publishing** for owner `Yubickari`, repository
   `TouchKeyboard`, workflow `release.yml`, environment `release`, and set the
   `NUGET_USER` secret (see [release.md](release.md)).
2. Confirm `CHANGELOG.md` and `<Version>0.2.0</Version>` are current, then commit
   and push a `v0.2.0` tag.
3. The release workflow runs the package smoke, preflight, pushes
   `.nupkg`/`.snupkg` with OIDC and creates the GitHub release.
4. Install `TouchKeyboard.Avalonia` (and optionally `TouchKeyboard.Ime.Pinyin`)
   from nuget.org into a clean Avalonia 12 project as the post-release smoke test.
5. Flip the repository to public once `0.2.0` is verified; the private-repo
   trusted-publishing policy only has a 7-day activation window.

## Version plan

| Version | Scope |
|---|---|
| `0.1.0-preview.1` / `0.1.0` | **Not released.** The baseline was never published; `0.2.0` is the first release. |
| `0.2.0` | First release: no-IME layout baseline, optional managed pinyin IME and language/text packs (ADR 0004), four packages, golden set and AOT/trim smoke. The real-device run is the remaining release gate. |
| `1.0.0` | Stable API, WPF frontend available, documented compatibility and support policy. |

## Candidate features (not committed)

- **Other language packs:** a future `TouchKeyboard.Ime.<Language>` pack can implement
  `IImeEngine` and reuse the Avalonia IME UI; no concrete language is committed for
  `0.2.0`. Multi-language switching UI remains out of scope.
- **Profiles:** `time` (separator `:`), `range` (two numbers), `currency` (suffix
  handling belongs to the host), IPv6 (multi-group), custom masks.
- **Numpad:** fill the unused right-row-2 slot for scenario keys, enable `Sequence`
  for serial-number entry, optional key-hint for long-press.
- **Full keyboard:** optional symbol-page entry for `AnsiStyle`; `KeyGlyph.Symbol`/
  `Numeric` renderings. The IME key and the 中/英 toggle are part of `0.2.0`, not
  candidates.
- **Frontend:** WPF port, high-contrast/high-visibility theme, configurable key
  sounds/haptics via host callbacks, screen-reader announcements for validation.
- **Tooling:** `dotnet format` gate, coverage reporting, benchmark project,
  automated screenshot generation in CI.

## Non-goals

- A separate `0.1.0` release or a `0.1.x` maintenance line.
- System IME bridging, key injection, cross-process input.
- Wubi, handwriting, fuzzy pinyin, cloud input, self-learning or
  Traditional/Simplified conversion.
- Multi-language switching UI, language marketplace, dynamic plugin discovery.
- System-level keyboard hooks or a global OSK replacement.
- F1–F12, NumLock keypad block, Ctrl/Alt/Win/Menu/PrintScreen keys.
- Multi-line editing, selection, clipboard and undo/redo.
- Mobile/WebAssembly targets, iOS/Android gesture models.
- Business validation (required business rules, cross-field checks) — the host's
  view model remains the source of truth.

## How priorities are decided

1. Field errors and layout/interaction problems reported from real devices.
2. Anything that blocks the `0.2.0` release.
3. Reusable-core improvements that a second frontend (WPF) would need.
4. Optional profiles and visual polish.

Feedback should go through the GitHub issue templates with the device checklist
filled in; layout changes are recorded in [layouts.md](layouts.md) and API changes in
[design/decisions.md](design/decisions.md).
