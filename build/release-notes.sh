#!/usr/bin/env bash
# Extract the current version's Chinese changelog section for the GitHub release.
# Writes .tmp/release-notes.md so the release workflow can pass it to
# softprops/action-gh-release as body_path.
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

version="$(sed -n 's:.*<Version>\(.*\)</Version>.*:\1:p' Directory.Build.props | head -n 1)"
if [[ -z "$version" ]]; then
  echo "release-notes: could not read <Version> from Directory.Build.props" >&2
  exit 1
fi

mkdir -p .tmp
python3 - "$version" <<'PY'
import re
import sys
from pathlib import Path

version = sys.argv[1]
text = Path("CHANGELOG.zh.md").read_text(encoding="utf-8")
pattern = re.compile(
    rf"^## \[{re.escape(version)}\].*?(?=^## \[[^\]]+\]|\Z)",
    re.MULTILINE | re.DOTALL,
)
match = pattern.search(text)
if match is None:
    raise SystemExit(f"release-notes: CHANGELOG.zh.md has no section for {version}")

section = match.group(0).strip()
footer = (
    "\n\n---\n\n"
    f"完整中文变更记录：[CHANGELOG.zh.md](https://github.com/Yubickari/TouchKeyboard/blob/v{version}/CHANGELOG.zh.md)  \n"
    f"English changelog：[CHANGELOG.md](https://github.com/Yubickari/TouchKeyboard/blob/v{version}/CHANGELOG.md)"
)
Path(".tmp/release-notes.md").write_text(section + footer + "\n", encoding="utf-8")
print(f"release-notes: wrote .tmp/release-notes.md for {version}")
PY
