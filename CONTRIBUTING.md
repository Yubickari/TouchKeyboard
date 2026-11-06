# Contributing

Thanks for taking a look. This project is small; the rules are short.

## Ground rules

- **Clean-room.** The code is an independent implementation. Do not copy source,
  comments, resources, screenshots, customer names or business field names from
  any company project into this repository.
- **No private context.** No company/customer/project names, no home-directory
  paths, no real device screenshots, no secrets. The preflight script scans for a
  local denylist via `TOUCHKEYBOARD_DENYLIST`; never commit that list.
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
dotnet pack src/TouchKeyboard.Core/TouchKeyboard.Core.csproj -c Release -o artifacts
```

## Style

- Comments explain *why*. A comment that restates the next line is noise.
- Public API uses the canonical names in `docs/design/decisions.md`.
- Chinese and English user-facing copy change together once localization exists.
- Keep the core dependency-free; adding a package to `TouchKeyboard.Core`
  requires an explicit decision.

## Pull requests

- Keep the change focused; include the test that would fail without it.
- Update `CHANGELOG.md` under `[Unreleased]` for user-visible changes.
- Run `build/preflight.sh` before tagging a release.
