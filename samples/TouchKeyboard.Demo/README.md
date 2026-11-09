# TouchKeyboard demo

**English** · [简体中文](README.zh.md)

Avalonia sample for TouchKeyboard. It wires the overlay host to a set of fields
that cover every built-in input profile and activates the optional pinyin
engine.

## Run

```bash
dotnet run --project samples/TouchKeyboard.Demo
```

The sample targets `net8.0` and references the Avalonia package from the
repository. Restore uses the standard NuGet cache; in a sandbox, point
`NUGET_PACKAGES` at a writable directory first.

## What to try

- Click a field to open the keyboard. The header shows its label and unit.
- Submit with **确定** or Enter; close with the × button, the scrim or Escape and
  the edit is discarded.
- Switch the full-keyboard geometry between `AnsiStyle` and `PhoneHybrid`, and
  switch the app theme between system, light and dark.
- Use the appearance panel to change overall/font/icon scale, semantic colours,
  panel dragging, the built-in key language (Auto/Chinese/English) and the
  keyboard font (platform default, CJK-first, monospace, serif).
- Open the Chinese field and type `nihao`, `beijing` or `woaibeijing` to try the
  candidate bar and inline composition. Space commits the highlighted candidate;
  the ‹ / › keys change candidate pages. Switch the key language to English and
  the IME toggle reads CN/EN; the geometry does not change.
- Check the profile-restricted fields: a positive integer has no sign or decimal
  point, a date uses its separator, and an IPv4 field limits each part to
  0–255.
- The password, read-only and disabled-keyboard fields exercise the adapter
  rules; read-only and disabled fields never open the panel.

## Project layout

| File | Purpose |
|---|---|
| `Program.cs` / `App.axaml` | Avalonia entry point and theme. |
| `MainWindow.axaml` | Fields, host overlay and appearance controls. |
| `MainWindow.axaml.cs` | Profile wiring, pinyin engine and appearance updates. |
