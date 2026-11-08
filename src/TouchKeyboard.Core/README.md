# TouchKeyboard.Core

Pure .NET Standard 2.0 core for [TouchKeyboard](https://github.com/Yubickari/TouchKeyboard),
a touch-first embedded software keyboard for small industrial touch screens.

The package contains the UI-free input model: input profiles, validation,
immutable keyboard layouts, the editing session, IME composition contracts and
culture-neutral validation errors. It has no UI, platform or package
dependencies, so a frontend can reuse it on any supported .NET target.

## Install

```bash
dotnet add package TouchKeyboard.Core
```

## What it contains

- `InputProfile` and `InputProfileRegistry`: the ten built-in profiles (`text`,
  `password`, `ascii-token`, `integer`, `positive-integer`, `decimal`,
  `positive-decimal`, `date-slash`, `date-dot`, `ip-address`) plus custom
  profiles.
- `KeyboardLayout`, `KeyboardRow`, `KeyboardGridLayout`, `KeyboardGridCell`,
  `KeyDefinition`, `KeyLabel`, `KeyStyle`, `KeyGlyph` and `KeyCommand`.
- `KeyboardSession`, `KeyboardSessionOptions` and immutable `KeyboardState`
  snapshots with commit/cancel and validation.
- `NumericKeypadOptions`, `NumpadSlots` and `NumpadSlotKey` for numeric keypad
  scenarios.
- `IImeEngine`, `ImeCandidate` and `ImeCompositionState` for optional,
  language-neutral IME composition.

## Minimal session

```csharp
using TouchKeyboard.Core;

var session = new KeyboardSession(new KeyboardSessionOptions
{
    Text = "42",
    Profile = InputProfiles.PositiveInteger,
    MaxLength = 10
});

session.Execute(KeyCommand.Insert("3"));

if (session.TryCommit(out var text, out var error))
{
    // The host writes `text` back to its control.
}
else
{
    // `error` is a culture-neutral InputValidationError.
}
```

## Documentation

- [Architecture](https://github.com/Yubickari/TouchKeyboard/blob/main/docs/architecture.md)
- [Input profiles](https://github.com/Yubickari/TouchKeyboard/blob/main/docs/profiles.md)
- [Layouts](https://github.com/Yubickari/TouchKeyboard/blob/main/docs/layouts.md)
- [Public API](https://github.com/Yubickari/TouchKeyboard/blob/main/docs/public-api.md)

## License

MIT. See [LICENSE](https://github.com/Yubickari/TouchKeyboard/blob/main/LICENSE).
