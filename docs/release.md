# Release process

Releases publish `TouchKeyboard.Core`, `TouchKeyboard.Avalonia`,
`TouchKeyboard.Ime.Pinyin` and `TouchKeyboard.Ime.Pinyin.Extended` to nuget.org,
plus a GitHub release carrying the `.nupkg`/`.snupkg` artifacts. Publishing uses
NuGet **Trusted Publishing** (OIDC), so no long-lived API key is stored anywhere.

## Version source

`Directory.Build.props` holds the single `<Version>` used by all four packages. A
release tag must be `v<Version>`, for example `v0.2.0`. `build/preflight.sh`
fails when the tag and version disagree, and (on a tag build) when the working tree
is dirty.

`TouchKeyboard.Avalonia` compiles against the Avalonia 12.0.5 compatibility floor,
so the published dependency is `Avalonia >= 12.0.5`. CI also runs the test suite
against 12.1.5. The release and preflight commands use the default 12.0.5 floor;
do not override `AvaloniaVersion` when packing a release.

## One-time setup

1. On nuget.org, create a Trusted Publishing policy for the package owner:

   | Field | Value |
   |---|---|
   | Owner | `Yubickari` |
   | Repository | `TouchKeyboard` |
   | Workflow file | `release.yml` |
   | Environment (optional) | `release` |

   A private repository's policy has a temporary activation window (about 7 days);
   the first publish must happen inside it, otherwise re-activate the policy. Public
   repositories get a permanent policy.

2. In the GitHub repository, add the secret `NUGET_USER` (the nuget.org account
   name). No API key secret is needed.

3. Create the `release` environment under **Settings → Environments** if the policy
   pins it. Add protection rules (required reviewers) if releases should be gated.

`release.yml` requests `id-token: write` (OIDC exchange) and `contents: write`
(GitHub release).

## Local rehearsal

```bash
# Tests, pack, package-content checks and hygiene:
bash build/preflight.sh

# Consume the produced packages from a local feed in two throwaway projects:
bash build/smoke-pack.sh
```

`smoke-pack.sh` also verifies that a plain console project can reference the core
package, that a minimal Avalonia 12 app can reference the frontend package without
pulling a language pack, that the Avalonia + Pinyin and Avalonia + Extended shapes
run a real query through the exported engine, and that a trimmed AOT publish can
load the dictionaries and commit a composition. The AOT step needs `clang` and
`zlib` (the CI workflow installs them on Ubuntu).

## Release checklist

1. Update `CHANGELOG.md` (the release notes are generated from it plus commits) and
   bump `<Version>` in `Directory.Build.props` if needed.
2. Review `README.md` / `README.zh.md` for install commands, version, links and
   screenshots; review the package READMEs under `src/*/README.md` for absolute
   links.
3. Run `bash build/preflight.sh` and `bash build/smoke-pack.sh` locally.
4. Commit, then create and push the tag:

   ```bash
   git tag v0.2.0
   git push origin v0.2.0
   ```

5. Watch the **Release** workflow. It runs the package smoke (three consumer
   shapes plus AOT), preflight, exchanges the OIDC token for a short-lived NuGet
   key, pushes all four packages (skipping duplicates) and creates the GitHub
   release with the artifacts attached.
6. Validate from the public feed in a clean project:

   ```bash
   dotnet new console -o /tmp/osk-smoke
   cd /tmp/osk-smoke
   dotnet add package TouchKeyboard.Avalonia --version 0.2.0
   dotnet build
   ```

   Then open the package page and check the icon, README rendering and symbol
   package.

## Expected package contents

| Package | Entries |
|---|---|
| `TouchKeyboard.Core` | `lib/netstandard2.0/TouchKeyboard.Core.dll`, `.xml`, `README.md`, `icon.png`, `.snupkg` alongside |
| `TouchKeyboard.Avalonia` | `lib/net8.0` and `lib/net10.0` `.dll` + `.xml`, `README.md`, `icon.png`, `.snupkg` alongside |
| `TouchKeyboard.Ime.Pinyin` | `lib/netstandard2.0` `.dll` + `.xml`, `README.md`, `icon.png`, `NOTICE`, `resources/pinyin-common.bin`, `.snupkg` alongside |
| `TouchKeyboard.Ime.Pinyin.Extended` | `lib/netstandard2.0` `.dll` + `.xml`, `README.md`, `icon.png`, `NOTICE`, `resources/pinyin-extended.bin`, `.snupkg` alongside |

Data packs use only permissive/attribution licences (no share-alike or copyleft)
and list their sources and snapshot hashes in `NOTICE`; there is no separate legal
gate. `preflight.sh` checks all four package shapes, the `NOTICE`/README text,
the dictionary resources and the Avalonia 12.0.5 dependency floor, and fails the
release when any entry is missing. Package
READMEs must use absolute URLs for images and links: NuGet does not resolve URLs
relative to the repository. The package smoke must still pass the no-engine
behavior matrix (including expanded Space) for the Avalonia-only shape. See
[ADR 0004](adr/0004-managed-pinyin-ime.md) and [ime.md](ime.md).

## Post-release validation

- [ ] All four package pages show the icon and README, and README links/images resolve; the two language packs show `NOTICE` in the package contents.
- [ ] `dotnet add package` resolves from nuget.org (not just the local feed).
- [ ] A clean Avalonia 12 app builds with the package and shows the keyboard.
- [ ] A clean Avalonia app with `TouchKeyboard.Ime.Pinyin` shows the 中/英 key and candidate row for a `text` field.
- [ ] The symbol packages are listed and sourcelink points at the tag commit.
- [ ] The GitHub release has all `.nupkg` and `.snupkg` files.

## Failure and rollback

- A published NuGet version cannot be deleted. Unlist it, then publish a fixed
  `X.Y.Z+1` (or a new prerelease) instead of reusing the version.
- If the workflow fails before the push, fix the cause and re-run the workflow;
  `--skip-duplicate` makes a partial push idempotent.
- If a token exchange fails, check the policy fields (owner/repository/workflow/
  environment), that the workflow runs from the default branch context, and the
  private-repo activation window.
- Keep the `release` environment protected so only reviewers can publish.

## Hygiene and visibility

Before making the repository public:

```bash
TOUCHKEYBOARD_DENYLIST="customer-name,internal-project" bash build/hygiene.sh
```

The scan covers home-directory paths, common secret patterns and an optional
denylist from `TOUCHKEYBOARD_DENYLIST`. `docs/local/` is ignored and must not be
published. Screenshots must contain fictional sample data only.

## Related documents

- [roadmap.md](roadmap.md) — future direction and non-goals.
- [adr/decisions.md](adr/decisions.md) — package, licence and API decisions.
- [release/device-checklist.md](release/device-checklist.md) — real-device acceptance.
- [architecture.md](architecture.md) — package layout and target frameworks.
