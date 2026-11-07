# Input profiles

An `InputProfile` is the named contract for one field: which keyboard mode it opens
in, whether the full keyboard is allowed, which numeric keypad keys apply, which
editing states are legal, and how a committed value is validated and normalized.
Profiles are immutable and safe to share; the `KeyboardSession` applies them.

## Built-in profiles

| Id | Default mode | Accepts | Commit normalization | Numeric keypad |
|---|---|---|---|---|
| `text` | Full keyboard | Any text without control characters | Unchanged | General (＋/− characters, ABC, decimal point) |
| `password` | Full keyboard | Printable ASCII `0x20`–`0x7E` | Unchanged | General |
| `ascii-token` | Full keyboard | `[A-Za-z0-9/_.:-]*`, non-empty | `Trim()` | General |
| `integer` | Numeric keypad | Signed integer | Kept as typed (leading zeros stay) | Step keys, ±, no decimal/padded `0` |
| `positive-integer` | Numeric keypad | Digits only | Kept as typed | Step keys, no sign, `0` spans two columns |
| `decimal` | Numeric keypad | Signed decimal with invariant `.` | Parsed and re-formatted invariantly (`.5` → `0.5`, `1.50` → `1.5`) | Step keys, ±, `.` separator |
| `positive-decimal` | Numeric keypad | Non-negative decimal | Same as `decimal` | Step keys, no sign, `.` separator |
| `date-slash` | Numeric keypad | `yyyy/M/d` parts while editing, valid date on commit | `yyyy/MM/dd` | Step keys, `/` separator |
| `date-dot` | Numeric keypad | Same with `.` | `yyyy/MM/dd` (normalized to slash for adapters) | Step keys, `.` separator |
| `ip-address` | Numeric keypad | Four 1–3 digit parts, values `0–255`, max 15 chars | Parts without leading zeros, `.`-joined | Step keys, `.` separator |

`InputProfileRegistry` resolves ids case-insensitively. `InputProfiles.All`
enumerates the built-ins in a stable order.

### Who supplies what

- The **profile** decides validity, the default mode, the numeric key set and the
  normalization.
- The **session** combines the profile with target metadata: effective maximum
  length (`min(profile limit, target limit)`), `Minimum`/`Maximum` range checks,
  and whether the target allows a negative sign.
- The **adapter** reads the target's current text and metadata and writes the
  committed text back.

So `integer` with a target `Minimum = 0` still accepts the ± key from the profile's
key set only if the target reports negative values; otherwise the session ignores
the toggle, and the adapter rejects out-of-range commits.

## Editing vs commit validation

Every profile implements two separate checks:

- `IsIntermediateValid(text)` — is this a legal state while typing? Examples: `-`,
  `1.`, `2026/` and `192.168.` are editable states even though they are not
  committable. Inserts that break this rule are dropped.
- `TryValidate(text, out normalized, out error)` — is this a legal committed value?
  It also returns the normalized text the adapter writes back.

A `KeyboardSession` calls the intermediate check on every insert, and the commit
check only in `TryCommit`.

## Validation errors

Core validation returns a culture-neutral `InputValidationError` (a code plus
arguments). The frontend localizes it; `FallbackMessage` is English-only.

| Code | Meaning |
|---|---|
| `EmptyNotAllowed` | The profile or target requires a value. |
| `InvalidInteger` | Text is not a signed integer. |
| `InvalidDecimal` | Text is not a valid decimal number (or a sign is not allowed). |
| `TooManyFractionDigits` | More fractional digits than the profile allows (argument: maximum). |
| `InvalidDate` | Parts are missing or the date does not exist. |
| `InvalidIpAddress` | Not four `0–255` parts. |
| `InvalidAsciiToken` | Characters outside `A-Z a-z 0-9 / _ . : -`, or empty. |
| `InvalidPassword` | Characters outside printable ASCII. |
| `ValueTooSmall` | Parsed value is below the session `Minimum` (argument: minimum). |
| `ValueTooLarge` | Parsed value is above the session `Maximum` (argument: maximum). |
| `MaxLengthExceeded` | Effective maximum length exceeded (argument: limit). |
| `CommitRejected` | The adapter or a read-back rejected the value (also used after cancel). |

The built-in `zh-CN` and `en-US` localizers map every code to a sentence; see
[avalonia.md](avalonia.md#localization).

## Numeric keypad key set

The Numpad surface is derived from `NumericKeypadOptions` through `NumpadSlots`:

| Scenario | L0/L1 | L2 (left row 2) | R2 (right row 2) | Bottom slot | `0` | Confirm |
|---|---|---|---|---|---|---|
| General (from the full keyboard) | `+` `−` characters | `ABC` | empty | `.` | 1 column | 2 rows |
| Positive integer | ▲ ▼ step | empty | empty | empty | 2 columns | 2 rows |
| Signed integer | ▲ ▼ step | `±` | empty | empty | 2 columns | 2 rows |
| Positive decimal | ▲ ▼ step | empty | empty | `.` | 1 column | 2 rows |
| Signed decimal | ▲ ▼ step | `±` | empty | `.` | 1 column | 2 rows |
| Date | ▲ ▼ step | empty | empty | `/` or `.` | 1 column | 2 rows |
| IPv4 | ▲ ▼ step | empty | empty | `.` | 1 column | 2 rows |

The mapping is produced by `NumpadSlots.From(profile.NumericKeypad)`:
`ShowSign` picks ± over ABC, `ShowDecimalPoint` wins over an explicit separator, and
`ShowStepKeys` switches the top-left pair between step arrows and `+`/`−` characters.
The geometry and span rules are documented in [layouts.md](layouts.md#numpad).

## Profile contract

| Member | Kind | Purpose |
|---|---|---|
| `Id` | abstract | Stable id used by `TouchKeyboardInput.Profile`. |
| `DefaultMode` | virtual | `FullKeyboard` by default; numeric profiles return `NumericKeypad`. |
| `AllowsFullKeyboard` | virtual | Numeric profiles return `false` (mode switching and ABC are hidden). |
| `NumericKeypad` | virtual | `NumericKeypadOptions` for the Numpad slot mapping. Defaults to `NumericKeypadOptions.General`. |
| `MaxLength` | virtual | Optional profile-level limit combined with the target limit. |
| `AllowEmpty` | virtual | Whether an empty value can be committed. |
| `IsIntermediateValid` | virtual | Legal editing states. |
| `TryValidate` | abstract | Commit validation and normalization. |

## Custom profiles

```csharp
public sealed class SerialNumberProfile : InputProfile
{
    public override string Id => "serial-number";
    public override bool AllowEmpty => false;
    public override KeyboardMode DefaultMode => KeyboardMode.FullKeyboard;

    public override bool IsIntermediateValid(string text)
        => text.Length <= 24 && text.All(c => char.IsLetterOrDigit(c) || c is '-' or '_');

    public override bool TryValidate(string text, out string normalized, out InputValidationError? error)
    {
        normalized = text.Trim();
        if (normalized.Length == 0)
        {
            error = new InputValidationError(InputValidationErrorCode.EmptyNotAllowed);
            return false;
        }

        error = null;
        return true;
    }
}

InputProfileRegistry.Register(new SerialNumberProfile());
```

For custom numeric fields, prefer the existing `NumericInputProfile` base or build
`NumericKeypadOptions` so the Numpad slot mapping keeps working:

```csharp
var keypad = new NumericKeypadOptions(
    showSign: false,
    showDecimalPoint: false,
    separator: '-',
    showStepKeys: true);

var slots = NumpadSlots.From(keypad);          // or new NumpadSlots(...) for a custom mapping
var layout = KeyboardLayouts.Numpad("numeric-serial", slots);
```

`KeyboardLayouts.Numpad(id, slots)` is the extension point for scenarios that are
not built in (for example a time field with a `:` separator and a right-row-2
scenario key).
