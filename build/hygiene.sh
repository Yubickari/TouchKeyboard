#!/usr/bin/env bash
# Repository hygiene: fail on local paths, obvious secrets, or a local denylist.
# The denylist is read from TOUCHKEYBOARD_DENYLIST (comma-separated) and is never
# committed. Run from anywhere; paths are resolved from this script.
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

scan_paths=()
for path in src samples tests docs .github; do
  if [[ -e "$path" ]]; then
    scan_paths+=("$path")
  fi
done
for path in README.md README.zh.md CHANGELOG.md CONTRIBUTING.md SECURITY.md \
            CODE_OF_CONDUCT.md LICENSE Directory.Build.props \
            Directory.Packages.props NuGet.Config; do
  if [[ -f "$path" ]]; then
    scan_paths+=("$path")
  fi
done

if [[ ${#scan_paths[@]} -eq 0 ]]; then
  echo "hygiene: nothing to scan"
  exit 0
fi

patterns=(
  '/Users/[A-Za-z0-9._-]+/'
  '/home/[A-Za-z0-9._-]+/'
  'C:\\Users\\'
  'ghp_[A-Za-z0-9]{20,}'
  'github_pat_[A-Za-z0-9_]{20,}'
  'BEGIN (RSA |EC |OPENSSH )?PRIVATE KEY'
)

failed=0
matches="$(grep -RInE \
  --binary-files=without-match \
  --exclude='2026-11-06-touch-keyboard-plan.md' \
  --exclude-dir=bin \
  --exclude-dir=obj \
  --exclude-dir=.tmp \
  --exclude-dir=.nuget \
  --exclude-dir=artifacts \
  -e "${patterns[0]}" \
  -e "${patterns[1]}" \
  -e "${patterns[2]}" \
  -e "${patterns[3]}" \
  -e "${patterns[4]}" \
  -e "${patterns[5]}" \
  "${scan_paths[@]}" 2>/dev/null || true)"
if [[ -n "$matches" ]]; then
  echo "hygiene: local paths or secret-like strings found:"
  echo "$matches"
  failed=1
fi

if [[ -n "${TOUCHKEYBOARD_DENYLIST:-}" ]]; then
  IFS=',' read -r -a tokens <<< "$TOUCHKEYBOARD_DENYLIST"
  for token in "${tokens[@]}"; do
    token="$(echo "$token" | xargs)"
    [[ -z "$token" ]] && continue
    token_matches="$(grep -RInF \
      --binary-files=without-match \
      --exclude='2026-11-06-touch-keyboard-plan.md' \
      --exclude-dir=bin \
      --exclude-dir=obj \
      --exclude-dir=.tmp \
      --exclude-dir=.nuget \
      --exclude-dir=artifacts \
      -- "$token" "${scan_paths[@]}" 2>/dev/null || true)"
    if [[ -n "$token_matches" ]]; then
      echo "hygiene: denylisted term '$token' found:"
      echo "$token_matches"
      failed=1
    fi
  done
fi

if [[ $failed -ne 0 ]]; then
  exit 1
fi

echo "hygiene: ok"
