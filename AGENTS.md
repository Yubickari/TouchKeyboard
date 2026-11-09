# Agent and contributor instructions

This file is the short, checked-in rulebook for automated agents and human
contributors. Read it before changing code or documentation.

## Documentation language

TouchKeyboard users are primarily Chinese-speaking integrators. English stays the
source language for code, comments and internal engineering notes, but every
document that a library user or host developer is expected to read must have a
Chinese version.

**Chinese is required** for:

- `README.md` → `README.zh.md`
- `CHANGELOG.md` → `CHANGELOG.zh.md` and the GitHub release body
- `docs/avalonia.md` → `docs/avalonia.zh.md` (integration guide)
- `docs/profiles.md` → `docs/profiles.zh.md` (input-profile reference)
- `docs/layouts.md` → `docs/layouts.zh.md` (layout diagrams and geometry)
- `docs/public-api.md` (public API; keep it Chinese-first)
- `docs/customization.md` (appearance and sizing)
- `docs/ime.md` (Chinese input)
- `samples/TouchKeyboard.Demo/README.md` → `README.zh.md` in the same folder
- `src/*/README.md` → `README.zh.md` in the same package folder
- Any new usage guide, API guide, layout reference or release-note page

**English-only is acceptable** for architecture and process material:

- `docs/architecture.md`
- `docs/roadmap.md`
- `docs/adr/**`
- `docs/release.md`, `docs/release/**`
- `docs/contributing/**`
- `CONTRIBUTING.md`, `CODE_OF_CONDUCT.md`, `SECURITY.md`

Rules for paired documents:

1. Use a separate `.zh.md` file; do not mix half-translated sections in one file.
2. Start each paired file with a language switch. The English file links to the
   Chinese file and the Chinese file links back.
3. Translate prose, headings, table headers, figure captions and image alt text.
   Keep code identifiers, commands, paths, package names, log output and literal
   key ids unchanged.
4. Update both versions in the same change. A user-visible behavior change is not
   complete with only one language.
5. A new user-facing document is not ready to release until its Chinese version
   exists. The release checklist repeats this rule.
6. `docs/local/` is local-only and must never be committed or published.

## Code and release rules

- `TouchKeyboard.Core` stays UI-free; run the architecture test after model changes.
- Add a behavior test for validation, session, layout or language changes.
- Update both changelogs under `[Unreleased]` for user-visible changes.
- Run `bash build/docs-check.sh` after touching user-facing documentation; CI runs it.
- Run `bash build/preflight.sh` before a release tag.
- Keep company/customer names, home paths, real-device screenshots and secrets out
  of the repository; see the hygiene script and `CONTRIBUTING.md`.
