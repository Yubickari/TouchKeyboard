# TouchKeyboard

**English** · [简体中文](README.zh.md)

A touch-first, embedded software keyboard for small industrial touch screens.
The input logic is a pure .NET Standard 2.0 backend; Avalonia is the first
frontend, and WPF is planned.

> **Status: `0.2.0` implementation complete (2026-11-08).** The core has the ten
> input profiles, the strict ANSI and phone-hybrid full keyboards, the 5×4 Numpad
> cell grid, the no-IME Space baseline, immutable layouts, session/validation and
> the optional managed IME composition. Avalonia has the overlay host, controller,
> custom editor surface, IME key, fixed candidate bar, inline composition, vector
> key glyphs, automatic light/dark theming, touch press feedback, built-in adapters,
> theme and zh-CN/en-US localizer. The optional `TouchKeyboard.Ime.Pinyin` and
> `.Extended` packs ship the managed pinyin engine and lexicons. `0.1.0` is not
> released; `0.2.0` is the first release. Real-device verification is still
> pending, so do not treat the build as production-validated.

## Why

- Small screens (8–10 inch) and industrial panels need large keys and a layout
  that does not depend on the operating system's on-screen keyboard.
- Text, integer, decimal, date and IP fields should expose only the keys they
  actually accept; a positive integer field should not show a sign or decimal
  point.
- The input rules must be testable without a UI so a second frontend can reuse
  them.

## Packages

| Package | Target | Purpose |
|---|---|---|
| `TouchKeyboard.Core` | `netstandard2.0` | Modes, profiles, layouts, session, validation, IME composition |
| `TouchKeyboard.Avalonia` | `net8.0;net10.0` | Avalonia host, keys, candidate bar, adapters, theme |
| `TouchKeyboard.Ime.Pinyin` | `netstandard2.0` | Optional managed pinyin engine + common lexicon |
| `TouchKeyboard.Ime.Pinyin.Extended` | `netstandard2.0` | Optional extended lexicon (~100k words) |
| `TouchKeyboard.Wpf` | future | WPF frontend, same core |

Two keyboard modes:

1. **Full keyboard** — the strict 15-unit `AnsiStyle` ANSI geometry by default
   (five rows with Tab/Caps reserved spaces; with no IME, Space expands to 7.0
   units over the former IME slot; with an engine and a `text` profile, Space is
   5.5 and a 1.5-unit 中/英 key occupies the slot). The four-row `PhoneHybrid`
   letters keyboard with its own symbol page is a retained alternative (3.5/2.5
   Space, 1-unit IME key). No F keys, numpad or system keys.
2. **Numeric keypad** — a 5×4 `Numpad` shared by the general surface and the
   profile-restricted ones. Empty slots merge into their neighbour (`0` widens
   over an empty decimal slot, Confirm stretches over an empty right-row2 slot)
   instead of rendering dead keys.

Closing the panel discards edits; only **Confirm**/Enter commits. The field
label and unit are shown above the keys.

![Strict ANSI full keyboard](docs/images/keyboard-full.png)

![Profile-restricted numeric keypad](docs/images/keyboard-numeric.png)

![Dark theme](docs/images/keyboard-full-dark.png)

![Chinese pinyin IME with candidate bar](docs/images/keyboard-ime-composition.png)

*Headless Skia renders from the 1024×700 harness. The overlay follows the window's
theme variant, so a dark shell gets the dark preset automatically; see
[docs/layouts.md](docs/layouts.md) for every geometry, the symbol page, the Numpad
slot table and the theme metrics.*

## What works now

- `InputProfile`: text, password, ASCII token, signed/positive integer,
  signed/positive decimal, date with `/` or `.`, and IPv4. `AllowsIme` is true
  only for `text`.
- Immutable `KeyboardLayout` data: the strict `AnsiStyle` and four-row
  `PhoneHybrid` full keyboards, the PhoneHybrid symbol page, the 5×4 `Numpad`
  as a real cell grid with spans, profile-restricted numeric keypads that omit
  sign, decimal point, separator or ABC when they are not valid, and the
  retained `Sequence` page.
- `KeyGlyph` semantic icons: backspace, clear, enter, shift (outline/filled),
  caret-bar arrows, step arrows with `+`/`−` signs, space and password
  visibility are typed icons that each frontend draws as a vector, with the
  localized label kept as the accessible name.
- `NumpadSlots` scenario mapping: step keys vs `+`/`−` characters, `ABC` vs `±`
  in the left row-2 slot, and the bottom decimal/date/IP separator.
- `KeyboardSession`: insert, backspace, clear, caret movement, per-digit
  step-up/down, sign toggle, sticky shift, mode switch, full-keyboard page
  switch, commit/cancel, max length and numeric range checks.
- Optional IME: `IImeEngine`, `ImeCandidate`, `ImeCompositionState` in Core;
  Chinese composition, candidate paging/selection, Space/1–9/Enter commit and
  full-width punctuation routing; `ime.toggle` in the full-keyboard slot.
- Avalonia: `TouchKeyboardHost` overlay with scrim, title/unit/error row, fixed
  key area and a candidate row (44 px on AnsiStyle, key-row height on PhoneHybrid); auto-attaching controller;
  custom-drawn `TouchKeyboardEditor` with inline composition and underline;
  pointer-captured `TouchKey` with long-press repeat and vector icons;
  `KeyboardLayoutView`; `IEditorSurface` injection; adapters for `TextBox`,
  `NumericUpDown` and `CalendarDatePicker`; a custom adapter registry; light/dark
  themes that follow the window variant; zh-CN/en-US localizer; touch-first chrome
  with no hover states.
- Compact appearance API: `options.Style.Scale/FontScale/IconScale` and semantic
  colours for the confirm key, selected candidate, panel, keys and text;
  `options.EnablePanelDrag` allows long-press title-row dragging inside the window.
- 135 core tests + 40 pinyin/extended tests + 48 Avalonia headless tests per
  target framework (446 executions).

Not implemented yet: WPF, and the real-device run (Windows/Linux/macOS touch,
CJK font fallback, DPI, OSK suppression). `0.1.0` is not released; `0.2.0` is the
first release.

## Chinese input (optional)

Install the optional pinyin pack and add one line:

```bash
dotnet add package TouchKeyboard.Avalonia --version 0.2.0
dotnet add package TouchKeyboard.Ime.Pinyin --version 0.2.0
```

```csharp
using TouchKeyboard.Avalonia;
using TouchKeyboard.Ime.Pinyin;

KeyboardHost.Configure(options => options.UseIme(new PinyinImeEngine()));
```

`text` fields get 中/英 input and a fixed candidate row; numeric, date, password
and ASCII fields are unchanged. Installing no language pack keeps the no-IME
behavior exactly (no IME key or candidate row, Space 7.0/3.5). The extended pack
only swaps the engine type:

```csharp
using TouchKeyboard.Ime.Pinyin.Extended;

KeyboardHost.Configure(options => options.UseIme(new ExtendedPinyinImeEngine()));
```

The engine is managed, dictionary-based and does not bridge the system IME or
inject keys. See [docs/ime.md](docs/ime.md) for options, the interaction table,
theme/localization/accessibility details, package sizes, data sources and
troubleshooting.

## Repository layout

```text
src/TouchKeyboard.Core/              pure backend
src/TouchKeyboard.Avalonia/          Avalonia frontend
src/TouchKeyboard.Ime.Pinyin/        optional pinyin engine + common lexicon
src/TouchKeyboard.Ime.Pinyin.Extended/ optional extended lexicon
samples/TouchKeyboard.Demo/          demo application (includes a Chinese field)
tests/TouchKeyboard.Core.Tests/
tests/TouchKeyboard.Ime.Pinyin.Tests/
tests/TouchKeyboard.Avalonia.Tests/
tools/build-pinyin-dict.py           reproducible dictionary build
assets/icon.png                      package icon
docs/architecture.md                 layering, model and data flow
docs/avalonia.md                     integration, options, theming, adapters
docs/ime.md                          Chinese input, language packs, licensing
docs/customization.md                sizing, semantic colours, panel drag
docs/public-api.md                   public API inventory and extension seams
docs/profiles.md                     input profiles, validation and numeric slots
docs/layouts.md                      layout reference
docs/roadmap.md                      release status and version plan
docs/release.md                      NuGet release runbook
docs/wpf-porting.md                  WPF frontend notes
docs/design/decisions.md             accepted decisions
docs/adr/                            architecture decision records
docs/testing/device-checklist.md     real-device acceptance
docs/images/                         rendered previews
build/                               preflight, smoke and hygiene scripts
```

## Build

Requires the .NET 8 and .NET 10 SDKs (and `clang`/`zlib` for the AOT smoke).

```bash
dotnet restore TouchKeyboard.sln
dotnet build TouchKeyboard.sln -c Release
dotnet test tests/TouchKeyboard.Core.Tests/TouchKeyboard.Core.Tests.csproj -c Release
dotnet test tests/TouchKeyboard.Ime.Pinyin.Tests/TouchKeyboard.Ime.Pinyin.Tests.csproj -c Release
dotnet test tests/TouchKeyboard.Avalonia.Tests/TouchKeyboard.Avalonia.Tests.csproj -c Release
bash build/preflight.sh
bash build/smoke-pack.sh
```

Run the demo:

```bash
dotnet run --project samples/TouchKeyboard.Demo
```

The demo has selectors for the full-keyboard geometry (AnsiStyle / PhoneHybrid)
and the app theme variant (system / light / dark), live overall/font/icon scale
sliders (inside a collapsible test panel), accent/confirm/selected-candidate/panel/key colour selectors, a
long-press drag toggle and reset buttons, plus a Chinese `text` field wired to the
pinyin engine. Use the Chinese field to exercise the candidate bar and press
feedback; use `PhoneHybrid` + the symbol/numeric keys to check the height model.

Restore uses the standard NuGet package cache. In sandboxed environments, set
`NUGET_PACKAGES` to a writable directory before restoring. The smoke script
defaults it to the repository's `.nuget/packages`.

## Documentation

- [Architecture](docs/architecture.md) — projects, core model, data flow, extension seams.
- [Avalonia integration](docs/avalonia.md) — setup, options, theming, adapters, troubleshooting.
- [Chinese input](docs/ime.md) — optional pinyin packs, candidate bar, licensing, sizes.
- [Customization](docs/customization.md) — scale, font/icon sizes, semantic colours and panel drag.
- [Public API](docs/public-api.md) — 0.2.0 API inventory, compatibility and extension seams.
- [Input profiles](docs/profiles.md) — built-in profiles, validation rules, numeric slots.
- [Layouts](docs/layouts.md) — ANSI/PhoneHybrid geometry, symbol page, Numpad grid.
- [Roadmap](docs/roadmap.md) — current release status, readiness checklist and version plan.
- [Release](docs/release.md) — versioning, Trusted Publishing, release runbook.
- [WPF porting notes](docs/wpf-porting.md) — what a second frontend needs.
- [Decision records](docs/adr/) and [accepted decisions](docs/design/decisions.md).
- [Chinese input design plan](docs/design/2026-11-08-chinese-input-and-language-packs.md) — managed pinyin route, optional language/text packs and the one-line API.
- [Device checklist](docs/testing/device-checklist.md) — real-device acceptance for 0.2.0.

## Known limitations

- The managed pinyin engine covers full pinyin, word/sentence segmentation and
  common abbreviations. It does not do Wubi, handwriting, fuzzy pinyin, cloud
  input, user learning or Traditional/Simplified conversion.
- Candidate quality is best-effort; the common lexicon is about 20k words and the
  extended pack about 100k. The golden set is a development target, not a quality
  guarantee.
- The editor is single-line: no selection, clipboard, undo or multi-line layout.
- Editing happens in the session buffer; the target control is written only on
  confirm. Closing the panel always discards.
- No F-keys, numpad/NumLock block, Ctrl/Alt/Win combinations or system keys.
- WPF is not implemented yet.
- Real-device behavior (CJK fonts, OSK suppression, DPI, 1024×600, dictionary load
  time/memory) must still be recorded on the checklist before calling `0.2.0`
  field-ready.

## License

MIT. See [LICENSE](LICENSE). Optional language/text data uses only permissive or
attribution licences (CC0, MIT, BSD, Apache-2.0, Unicode License or CC-BY 4.0) with
a short `NOTICE`; no CC-BY-SA, GPL/AGPL, proprietary or unknown-provenance data is
shipped. The pinyin lexicons use Unihan `kMandarin` readings (Unicode License V3)
and the jieba word list/frequencies (MIT); each language package carries a
`NOTICE` with sources, snapshot hashes and full licence texts. The project adds no
commercial restriction: anyone may use, modify and redistribute it for free.

The software and optional data are provided as-is; no input quality or stability
guarantee is made. Maintenance is best-effort.
