# Changelog

All notable changes to this project are documented in this file.
The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

- P1 walking skeleton: solution, `TouchKeyboard.Core` and
  `TouchKeyboard.Avalonia` projects, demo app, core test project.
- Package metadata, central package versions, CI workflow and release preflight.
- Accepted decision record.
- P2 core: ten built-in `InputProfile` instances (text, password, ASCII token,
  signed/positive integer, signed/positive decimal, date `/`, date `.`, IPv4),
  immutable `KeyboardLayout` data (PhoneHybrid, general Numpad/Phone,
  profile-restricted numeric keypads), `KeyboardSession` (insert, backspace,
  clear, caret, per-digit step, sign, shift, mode switch, commit/cancel,
  max-length and range checks), culture-neutral validation errors and an input
  profile registry.
- 78 core tests per target framework, including a key-set matrix that proves
  irrelevant numeric keys are absent rather than disabled.

## [0.1.0-preview.1] - 2026-11-06

Not released yet.

[Unreleased]: https://github.com/Yubickari/TouchKeyboard/commits/main
[0.1.0-preview.1]: https://github.com/Yubickari/TouchKeyboard/releases/tag/v0.1.0-preview.1
