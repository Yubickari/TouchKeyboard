#!/usr/bin/env bash
# Enforce the documentation language policy from AGENTS.md.
# User-facing documents must have a Chinese version; architecture/process docs
# may stay English-only.
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

required_chinese_docs=(
  README.zh.md
  CHANGELOG.zh.md
  docs/avalonia.zh.md
  docs/profiles.zh.md
  docs/layouts.zh.md
  docs/public-api.md
  docs/customization.md
  docs/ime.md
  samples/TouchKeyboard.Demo/README.zh.md
  src/TouchKeyboard.Core/README.zh.md
  src/TouchKeyboard.Avalonia/README.zh.md
  src/TouchKeyboard.Ime.Pinyin/README.zh.md
  src/TouchKeyboard.Ime.Pinyin.Extended/README.zh.md
)

failed=0
for doc in "${required_chinese_docs[@]}"; do
  if [[ ! -s "$doc" ]]; then
    echo "docs-check: missing Chinese document $doc" >&2
    failed=1
  fi
done

# Paired English documents should point at their Chinese counterpart, and the
# Chinese page should point back. Keep this light: the link text is the contract.
for pair in \
  "README.md:README.zh.md" \
  "CHANGELOG.md:CHANGELOG.zh.md" \
  "docs/avalonia.md:docs/avalonia.zh.md" \
  "docs/profiles.md:docs/profiles.zh.md" \
  "docs/layouts.md:docs/layouts.zh.md" \
  "samples/TouchKeyboard.Demo/README.md:samples/TouchKeyboard.Demo/README.zh.md"; do
  english="${pair%%:*}"
  chinese="${pair##*:}"
  if [[ -s "$english" ]] && ! grep -Fq "[简体中文]($(basename "$chinese"))" "$english"; then
    echo "docs-check: $english does not link to its Chinese version" >&2
    failed=1
  fi
  if [[ -s "$chinese" ]] && ! grep -Fq "[English]($(basename "$english"))" "$chinese"; then
    echo "docs-check: $chinese does not link back to $english" >&2
    failed=1
  fi
done

# Package pages use an absolute GitHub URL for the Chinese link (NuGet does not
# ship the .zh.md file), so check their switches separately.
for package in \
  src/TouchKeyboard.Core \
  src/TouchKeyboard.Avalonia \
  src/TouchKeyboard.Ime.Pinyin \
  src/TouchKeyboard.Ime.Pinyin.Extended; do
  if ! grep -Fq "简体中文" "$package/README.md"; then
    echo "docs-check: $package/README.md does not link to its Chinese version" >&2
    failed=1
  fi
  if ! grep -Fq "[English](README.md)" "$package/README.zh.md"; then
    echo "docs-check: $package/README.zh.md does not link back to English" >&2
    failed=1
  fi
done

if [[ $failed -ne 0 ]]; then
  exit 1
fi

echo "docs-check: ok"
