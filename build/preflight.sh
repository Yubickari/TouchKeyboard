#!/usr/bin/env bash
# Local release preflight. Mirrors the checks a tag release will run, in the
# same order, so failures are found before a tag exists.
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

export NUGET_HTTP_CACHE_PATH="${NUGET_HTTP_CACHE_PATH:-$ROOT/.nuget/http-cache}"

version="$(sed -n 's:.*<Version>\(.*\)</Version>.*:\1:p' Directory.Build.props | head -n 1)"
if [[ -z "$version" ]]; then
  echo "preflight: could not read <Version> from Directory.Build.props" >&2
  exit 1
fi
echo "preflight: version $version"

if [[ "${GITHUB_REF_NAME:-}" == v* ]]; then
  tag_version="${GITHUB_REF_NAME#v}"
  if [[ "$tag_version" != "$version" ]]; then
    echo "preflight: tag $GITHUB_REF_NAME does not match version $version" >&2
    exit 1
  fi

  if [[ -n "$(git status --porcelain)" ]]; then
    echo "preflight: working tree is dirty; commit before tagging a release" >&2
    exit 1
  fi
fi

echo "preflight: tests"
dotnet test tests/TouchKeyboard.Core.Tests/TouchKeyboard.Core.Tests.csproj -c Release
dotnet test tests/TouchKeyboard.Avalonia.Tests/TouchKeyboard.Avalonia.Tests.csproj -c Release

echo "preflight: pack"
rm -rf artifacts
dotnet pack src/TouchKeyboard.Core/TouchKeyboard.Core.csproj -c Release -o artifacts
dotnet pack src/TouchKeyboard.Avalonia/TouchKeyboard.Avalonia.csproj -c Release -o artifacts

core_package="artifacts/TouchKeyboard.Core.$version.nupkg"
avalonia_package="artifacts/TouchKeyboard.Avalonia.$version.nupkg"

for package in "$core_package" "$avalonia_package"; do
  if [[ ! -f "$package" ]]; then
    echo "preflight: missing package $package" >&2
    exit 1
  fi
done

echo "preflight: package contents"
mkdir -p .tmp
listing=".tmp/preflight-contents.txt"
trap 'rm -f "$listing"' EXIT

require_entry() {
  local package="$1"
  local entry="$2"
  unzip -Z1 "$package" > "$listing"
  if ! grep -qx "$entry" "$listing"; then
    echo "preflight: $package is missing $entry" >&2
    exit 1
  fi
}

require_entry "$core_package" 'lib/netstandard2.0/TouchKeyboard.Core.dll'
require_entry "$core_package" 'lib/netstandard2.0/TouchKeyboard.Core.xml'
require_entry "$core_package" 'README.md'
require_entry "$core_package" 'icon.png'
require_entry "$avalonia_package" 'lib/net8.0/TouchKeyboard.Avalonia.dll'
require_entry "$avalonia_package" 'lib/net8.0/TouchKeyboard.Avalonia.xml'
require_entry "$avalonia_package" 'lib/net10.0/TouchKeyboard.Avalonia.dll'
require_entry "$avalonia_package" 'lib/net10.0/TouchKeyboard.Avalonia.xml'
require_entry "$avalonia_package" 'README.md'
require_entry "$avalonia_package" 'icon.png'

echo "preflight: hygiene"
bash build/hygiene.sh

echo "preflight: ok ($version)"
