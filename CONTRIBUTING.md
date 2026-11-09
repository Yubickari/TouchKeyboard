# Contributing

Thanks for taking a look. This project is small; the rules are short.

## Ground rules

- **Clean-room.** The code is an independent implementation. Do not copy source,
  comments, resources, screenshots, customer names or business field names from
  any company project into this repository.
- **No private context.** No company/customer/project names, no home-directory
  paths, no real device screenshots, no secrets. The preflight script scans for a
  local denylist via `TOUCHKEYBOARD_DENYLIST`; never commit that list.
- **Local notes stay local.** `docs/local/` is ignored; keep meeting notes, drafts
  and customer-specific screenshots there (or outside the repository).
- **Core stays UI-free.** `TouchKeyboard.Core` must not reference Avalonia, WPF,
  platform APIs, timers or dispatchers. `TouchKeyboard.Avalonia` owns rendering,
  focus and control adapters.
- **Tests describe behaviour.** Add a rule test when changing validation or
  session semantics. A layout change needs a key-set assertion.

## Build and test

```bash
dotnet restore TouchKeyboard.sln
dotnet build TouchKeyboard.sln -c Release
dotnet test tests/TouchKeyboard.Core.Tests/TouchKeyboard.Core.Tests.csproj -c Release
dotnet test tests/TouchKeyboard.Avalonia.Tests/TouchKeyboard.Avalonia.Tests.csproj -c Release
bash build/docs-check.sh     # documentation language policy
bash build/preflight.sh      # tests + pack + package contents + hygiene
bash build/smoke-pack.sh     # consume the packages from a local feed
```

The default Avalonia version is the shipped compatibility floor (12.0.5). CI also
runs the suite against 12.1.5; pass `-p:AvaloniaVersion=12.1.5` to build or test
that combination locally.

## Documentation language

TouchKeyboard users are primarily Chinese-speaking integrators, so every
user-facing document must have a Chinese version. English remains the source
language for code comments, architecture, ADRs and internal process notes.

- **Paired with `.zh.md`:** `README.md`, `CHANGELOG.md`, `docs/avalonia.md`,
  `docs/profiles.md`, `docs/layouts.md`, `samples/TouchKeyboard.Demo/README.md`
  and each `src/*/README.md` package page.
- **Chinese-first:** `docs/public-api.md`, `docs/customization.md`,
  `docs/ime.md` and `docs/release/device-checklist.md`.
- **May stay English-only:** `docs/architecture.md`, `docs/roadmap.md`,
  `docs/adr/**`, `docs/release.md`, `docs/contributing/**` and repository
  governance files.
- Update both language versions in the same change. Translate prose, headings,
  table headers and captions; keep code identifiers, commands, paths, package
  names and literal key ids unchanged.
- New user-facing docs are not ready to release until the Chinese version exists.
  The full rule lives in [AGENTS.md](AGENTS.md).

## Style

- Comments explain *why*. A comment that restates the next line is noise.
- Public API uses the canonical names in `docs/adr/decisions.md`.
- Chinese and English user-facing copy change together.
- Keep the core dependency-free; adding a package to `TouchKeyboard.Core`
  requires an explicit decision.

## Pull requests

- Keep the change focused; include the test that would fail without it.
- Update `CHANGELOG.md` and `CHANGELOG.zh.md` under `[Unreleased]` for
  user-visible changes.
- Update the Chinese and English versions of every touched user-facing document.
- Run `build/preflight.sh` before tagging a release.
- Read [docs/architecture.md](docs/architecture.md), [docs/profiles.md](docs/profiles.md)
  and [docs/avalonia.md](docs/avalonia.md) before changing the model, a profile or
  the frontend wiring.
- Real-device changes should follow [docs/release/device-checklist.md](docs/release/device-checklist.md).
