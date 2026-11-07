# Roadmap

Status as of 2026-11-07. Versions are intentionally pre-release; APIs may still
change until `0.1.0`.

## Current state

| Milestone | State | Notes |
|---|---|---|
| P0 — decisions | Done | [decisions.md](design/decisions.md), D1–D16. |
| P1 — skeleton, CI, preflight | Done | Solution, packages, CI matrix, `preflight.sh`, `smoke-pack.sh`. |
| P2 — core | Done | Profiles, layouts, Numpad slots, session, validation, architecture test. |
| P3 — Avalonia frontend | Done | Host, controller, keys, editor surface, adapters, theme, localizer, demo. |
| Layout redesign | Done | `AnsiStyle` default, four-row `PhoneHybrid` with symbol page, 5×4 `Numpad` grid, retained `Sequence`. |
| Review refinements | Done | Dark mode following, touch press feedback, optical label offset, shared glyph canvas, ± glyph, caret-follows-spaces. |
| P4 — hardening | In progress | Remaining items below. |
| P5 — preview release | Not started | Workflow prepared; publishing credentials still to configure. |

## P4 — hardening checklist

- [x] Core and headless test suites (101 core + 23 Avalonia per target).
- [x] Layout reference, architecture, Avalonia, profile, release, WPF and roadmap docs.
- [x] ADRs for core purity, TFM matrix and the no-IME decision.
- [x] Package icon, package contents checks, issue templates, release workflow.
- [x] Package smoke (local-feed install) wired into CI and release.
- [x] Windows/Linux CI matrix green; macOS job gated to public repositories and
      verified locally until the repository is public.
- [ ] Real-device verification: 8″ 1280×800 and 10″ 1024×600 Windows touch,
      Ubuntu X11/Wayland, macOS; DPI 100/125/150/200%. Use
      [testing/device-checklist.md](testing/device-checklist.md).
- [ ] Confirm system OSK suppression and physical-keyboard isolation on Windows.
- [ ] 1024×600 screenshots and a short known-limitations section in both READMEs.
- [ ] Optional: `dotnet format --verify-no-changes`, dependency vulnerability scan,
      first-frame/key-latency measurements.

## P5 — preview release

1. Configure nuget.org **Trusted Publishing** for owner `Yubickari`, repository
   `TouchKeyboard`, workflow `release.yml`, environment `release`, and set the
   `NUGET_USER` secret (see [release.md](release.md)).
2. Update `CHANGELOG.md`, bump `<Version>` in `Directory.Build.props`, commit and
   push a `vX.Y.Z` tag.
3. The release workflow runs the package smoke, preflight, pushes `.nupkg`/`.snupkg`
   with OIDC and creates the GitHub release.
4. Install `TouchKeyboard.Avalonia` from nuget.org into a clean Avalonia 12 project
   as the post-release smoke test.
5. Flip the repository to public once the preview is verified; the private-repo
   trusted-publishing policy only has a 7-day activation window.

## Version plan

| Version | Scope |
|---|---|
| `0.1.0-preview.1` | Core + Avalonia + demo + docs + NuGet preview; API may change. |
| `0.1.0` | Layout/interaction fixes from field trials; API effectively stable. |
| `0.2.0` | More profiles (time, range, currency), scenario keys in the Numpad right row 2 (e.g. time `:`), high-contrast theme, accessibility improvements, WPF evaluation, optional IME integration package. |
| `1.0.0` | Stable API, WPF frontend available, documented compatibility and support policy. |

## Candidate features (not committed)

- **Profiles:** `time` (separator `:`), `range` (two numbers), `currency` (suffix
  handling belongs to the host), IPv6 (multi-group), custom masks.
- **Numpad:** fill the unused right-row-2 slot for scenario keys, enable `Sequence`
  for serial-number entry, optional key-hint for long-press.
- **Full keyboard:** optional symbol-page entry for `AnsiStyle`, IME/中英 toggle in
  the reserved slot, `KeyGlyph.Symbol`/`Numeric`/`Ime` renderings.
- **Frontend:** WPF port, high-contrast/high-visibility theme, configurable key
  sounds/haptics via host callbacks, screen-reader announcements for validation.
- **Tooling:** `dotnet format` gate, coverage reporting, benchmark project,
  automated screenshot generation in CI.

## Non-goals for v1

- Chinese/IME input, candidate windows, key injection into other processes.
- System-level keyboard hooks or a global OSK replacement.
- F1–F12, NumLock keypad block, Ctrl/Alt/Win/Menu/PrintScreen keys.
- Multi-line editing, selection, clipboard and undo/redo.
- Mobile/WebAssembly targets, iOS/Android gesture models.
- Business validation (required business rules, cross-field checks) — the host's
  view model remains the source of truth.

## How priorities are decided

1. Field errors and layout/interaction problems reported from real devices.
2. Anything that blocks the preview release (P4/P5 items above).
3. Reusable-core improvements that a second frontend (WPF) would need.
4. Optional profiles and visual polish.

Feedback should go through the GitHub issue templates with the device checklist
filled in; layout changes are recorded in [layouts.md](layouts.md) and API changes in
[design/decisions.md](design/decisions.md).
