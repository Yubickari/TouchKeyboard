# Avalonia integration

`TouchKeyboard.Avalonia` renders the keyboard as an in-window overlay. It targets
Avalonia 12 on `net8.0` and `net10.0`. The core package is UI-free, so the host
application keeps full control over its window and controls.

```bash
dotnet add package TouchKeyboard.Avalonia --version 0.2.0
```

## Minimal setup

Put the host **last** in the window's root grid so its scrim covers the page, then
mark the fields with attached properties:

```xml
<Window xmlns="https://github.com/avaloniaui"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:tk="using:TouchKeyboard.Avalonia"
        x:Class="MyApp.MainWindow">
  <Grid>
    <StackPanel Margin="16" Spacing="12">
      <TextBox tk:TouchKeyboardInput.Profile="text"
               tk:TouchKeyboardInput.Label="名称"
               PlaceholderText="名称" />

      <NumericUpDown tk:TouchKeyboardInput.Profile="positive-decimal"
                     tk:TouchKeyboardInput.Label="温度"
                     tk:TouchKeyboardInput.Unit="℃"
                     Minimum="0" Maximum="100" Value="25" />

      <TextBox tk:TouchKeyboardInput.Profile="password"
               tk:TouchKeyboardInput.Label="密码"
               PasswordChar="*" />
    </StackPanel>

    <tk:TouchKeyboardHost x:Name="KeyboardHost" />
  </Grid>
</Window>
```

No code-behind is required. `TouchKeyboardHost.AutoAttach` (default `true`) creates a
`TouchKeyboardController` when the host is attached to a window. The controller
watches pointer presses on the window, resolves the editable target, opens the
panel and restores focus when the panel closes.

### Attached properties

| Property | Type | Meaning |
|---|---|---|
| `TouchKeyboardInput.Profile` | `string?` | Input profile id, e.g. `positive-decimal` or `date-dot`. Unknown ids fall back to the control-type default. |
| `TouchKeyboardInput.Label` | `string?` | Field description shown in the title row. Falls back to the localized "Edit". |
| `TouchKeyboardInput.Unit` | `string?` | Unit shown next to the label, e.g. `℃`. |
| `TouchKeyboardInput.Enabled` | `bool` | Per-control switch (default `true`); `false` disables the keyboard for the control and its descendants. |

Without an explicit profile, the controller resolves one from the control type:
`CalendarDatePicker` → date profile (`.` if its custom format contains a dot),
`NumericUpDown` → `positive-decimal` when `Minimum >= 0`, otherwise `decimal`,
`TextBox` with `PasswordChar` → `password`, other editable controls → `text`.
Clicks that start inside a `Button`, read-only fields and disabled fields never
open the panel.

## Configuration

```csharp
public MainWindow()
{
    InitializeComponent();
    KeyboardHost.Configure(options =>
    {
        options.FullKeyboardLayout = FullKeyboardLayoutKind.PhoneHybrid;
        options.NumericKeypadLayout = NumericKeypadLayoutKind.Numpad;
        options.KeyHeight = 64;            // 0 = 56/64/72 from window height
        options.MaxPanelWidth = 1100;
        options.NumericPanelMaxWidth = 640;
        options.Culture = "zh-CN";         // built-in zh-CN / en-US
        options.HandlePhysicalKeyboard = true;
    });
}
```

| Option | Default | Notes |
|---|---|---|
| `Enabled` | `true` | Global switch; when false the controller does not open the panel. |
| `Theme` | `null` | `null` follows the window's actual theme variant; set `TouchKeyboardTheme.Light`/`.Dark` (or a custom instance) to pin one. |
| `FullKeyboardLayout` | `AnsiStyle` | `AnsiStyle` or `PhoneHybrid`. |
| `NumericKeypadLayout` | `Numpad` | `Numpad` is the enabled surface; `Sequence` is retained for custom scenarios. |
| `MaxPanelWidth` | `1280` | Panel width cap in logical pixels; multiplied by `Style.Scale`. |
| `NumericPanelMaxWidth` | `720` | Width cap for the numeric keypad area; multiplied by `Style.Scale`. |
| `KeyHeight` | `0` | Base key height; `0` selects 56/64/72 from the window height. Multiplied by `Style.Scale`, minimum 44 px. |
| `Style` | `Scale=1` | Compact sizing/palette facade; see [customization.md](customization.md). |
| `EnablePanelDrag` | `true` | Long-press the title row to drag the panel inside the window. |
| `HandlePhysicalKeyboard` | `true` | While open: Escape closes, Enter confirms, Backspace/arrows/up/down edit, text inserts. |
| `SuppressSystemKeyboard` | `true` | Reserved; v1 suppresses the platform OSK by moving focus to the custom-drawn host instead of using a text input. |
| `Culture` | `null` | Culture name for the built-in localizer; `null` follows the current UI culture. |
| `Localizer` | `null` | Custom `ITouchKeyboardLocalizer`; wins over `Culture`. |
| `EditorSurfaceFactory` | `null` | Factory for a custom `IEditorSurface`; defaults to the custom-drawn `TouchKeyboardEditor`. |
| `Ime` | engine `null` | Optional managed IME configuration; see [ime.md](ime.md). |

## Optional Chinese input

Install the optional language pack and activate it with one line:

```bash
dotnet add package TouchKeyboard.Ime.Pinyin --version 0.2.0
```

```csharp
using TouchKeyboard.Ime.Pinyin;

KeyboardHost.Configure(options => options.UseIme(new PinyinImeEngine()));
```

`text` fields then get a 中/英 key and a candidate row (44 px on AnsiStyle; on
PhoneHybrid it is the first of five rows and uses the key height). Numeric, date,
password and ASCII profiles stay IME-free. With no engine there is no IME key or
candidate row and Space fills the former IME slot (AnsiStyle 7.0 units,
PhoneHybrid 3.5); with an engine and an IME-capable profile Space returns to
5.5/2.5 and the key stays present in English mode, so toggling does not change the
width. The candidate row never scrolls: it displays as many complete candidate
labels as fit (up to nine) and adapts the Core page size; switch pages with the
56 px 切页 keys. The base `TouchKeyboard.Avalonia` package never references a language pack.
See [ime.md](ime.md), [ADR 0004](adr/0004-managed-pinyin-ime.md) and the
[design plan](design/2026-11-08-chinese-input-and-language-packs.md).

## Theming

The overlay resolves `TouchKeyboardTheme.Light` or `.Dark` from the host window's
`ActualThemeVariant` and re-applies when the variant changes, so a dark shell gets
the dark keyboard automatically. Close/confirm contrast comes from
`DangerKeyForeground` and `PrimaryKeyForeground`.

```csharp
KeyboardHost.Configure(options =>
{
    // Automatic is the default; pin a preset instead:
    options.Theme = TouchKeyboardTheme.Dark;

    // Or derive a custom preset from the built-in one:
    options.Theme = new TouchKeyboardTheme
    {
        KeyBackground = new SolidColorBrush(Color.Parse("#FF111827")),
        ActionKeyBackground = new SolidColorBrush(Color.Parse("#FF1F2937")),
        PanelBackground = new SolidColorBrush(Color.Parse("#FF0B1220")),
        Text = Brushes.White,
        PressedOverlay = new SolidColorBrush(Color.Parse("#33FFFFFF"))
    };
});
```

Press feedback is an overlay (`PressedOverlay`) plus a small scale
(`PressedScale`); no key has a hover state. `KeyLabelOffset` nudges labels down to
compensate for font metrics.

For a small set of knobs use `options.Style` (`Scale`, `FontScale`, `IconScale`,
`AccentColor`, `ConfirmColor`, `ConfirmTextColor`, `SelectedCandidateColor`,
`SelectedCandidateTextColor`, `PanelColor`, `KeyColor`, `TextColor`). It is applied
on top of the resolved theme and leaves the defaults untouched. The full
`TouchKeyboardOptions.Theme` remains available for every brush and metric. See
[customization.md](customization.md).

### Panel dragging

Long-press the title row (about 420 ms), then drag. The panel is clamped to the
current window; if it already fills the window there is little or no room. The
position is preserved for the host lifetime and is clamped on window resize.
`KeyboardHost.ResetPanelPosition()` restores the default bottom-centred position.
Set `options.EnablePanelDrag = false` to turn it off; the close button keeps its
normal tap behavior.

The optional IME adds `CandidateBarHeight` (default 44; PhoneHybrid first-row mode uses the key height), `CandidateBackground`,
`CandidateSelectedBackground`, `CandidateText`, `CandidateSelectedText`,
`CandidateMutedText`, `CandidateCornerRadius`, `CandidateFontSize`,
`CompositionText`, `CompositionUnderline` and `CompositionUnderlineThickness`.
Both light and dark presets ship values. The candidate bar keeps its height while
composing, paging or toggling 中/英.

## Localization

`ITouchKeyboardLocalizer` has two jobs: resolve key label resource keys and format
core validation errors.

```csharp
KeyboardHost.Configure(options => options.Localizer = new MyLocalizer());
```

Built-in cultures are `zh-CN` and `en-US`. Core validation never returns sentences;
it returns `InputValidationError` codes that the localizer turns into text. To add a
language, implement the interface and map the codes listed in
[profiles.md](profiles.md#validation-errors).

The IME adds keys such as `key.ime.active`/`key.ime.inactive`,
`key.ime.active.name`/`key.ime.inactive.name`, `ime.candidates`, `ime.previous`,
`ime.next`, `ime.no-candidate`, `ime.hint.chinese`, `ime.hint.english`,
`ime.candidate.name`, `ime.page` and `ime.composing`. Custom localizers should fill
them; `Get` returns the resource key when a key is missing.

## Custom editor surface

The default editor is `TouchKeyboardEditor`, a custom-drawn single-line control that
avoids the platform text input. A host may inject its own control:

```csharp
public sealed class ReadOnlyPreviewSurface : IEditorSurface
{
    private readonly TextBox _view = new TextBox { IsReadOnly = true, Focusable = false };
    private KeyboardSession? _session;

    public Control View => _view;

    public void Attach(KeyboardSession session)
    {
        Detach();
        _session = session;
        _view.Text = session.State.Text;
        session.StateChanged += OnStateChanged;
    }

    public void Detach()
    {
        if (_session is not null)
        {
            _session.StateChanged -= OnStateChanged;
            _session = null;
        }
    }

    private void OnStateChanged(object? sender, KeyboardStateChangedEventArgs e)
        => _view.Text = e.State.Text;
}

KeyboardHost.Configure(options => options.EditorSurfaceFactory = () => new ReadOnlyPreviewSurface());
```

A real editable control used as the surface may bring back the platform OSK/IME;
that is the trade-off the plan's D3 decision describes. A custom surface does not
have to render the IME composition, but the candidate row no longer repeats the raw
pinyin string; the surface should render composition itself if it needs to show it.
The `IEditorSurface` contract has no new members.

## Custom adapter

Adapters bridge a control and a session. Built-in adapters cover `TextBox`,
`NumericUpDown` and `CalendarDatePicker`. For a custom control, implement
`IEditorAdapter` and register a factory once at startup:

```csharp
public sealed class TemperatureAdapter : IEditorAdapter
{
    private readonly MyTemperatureField _field;

    public TemperatureAdapter(MyTemperatureField field, InputProfile profile)
    {
        _field = field;
        Profile = profile;
    }

    public Control Control => _field;
    public InputProfile Profile { get; }
    public string GetText() => _field.ValueText;
    public int MaxLength => 0;
    public bool AllowsNegativeSign => _field.AllowsNegative;
    public decimal? Minimum => (decimal?)_field.Minimum;
    public decimal? Maximum => (decimal?)_field.Maximum;

    public bool TryCommit(string text, out InputValidationError? error)
    {
        error = null;
        _field.ValueText = text;
        return true;
    }
}

TouchKeyboardAdapterRegistry.Register<MyTemperatureField>(
    (field, profile) => new TemperatureAdapter(field, profile));
```

## Custom profiles

Subclass `InputProfile` and register it:

```csharp
public sealed class SerialNumberProfile : InputProfile
{
    public override string Id => "serial-number";
    public override bool AllowEmpty => false;
    public override bool IsIntermediateValid(string text)
        => text.Length <= 24 && text.All(c => char.IsLetterOrDigit(c) || c is '-' or '_');

    public override bool TryValidate(string text, out string normalized, out InputValidationError? error)
    {
        normalized = text.Trim();
        error = normalized.Length == 0
            ? new InputValidationError(InputValidationErrorCode.EmptyNotAllowed)
            : null;
        return error is null;
    }
}

InputProfileRegistry.Register(new SerialNumberProfile());
```

Use it through `tk:TouchKeyboardInput.Profile="serial-number"`. See
[profiles.md](profiles.md) for the contract and the built-in set.

## Accessibility

Keys are `Border`-based rather than buttons, so a normal `Border` would be reported
as a non-control element. `TouchKey` therefore provides its own automation peer with
`AutomationControlType.Button` and a localized accessible name (icon keys use their
label resource key; the IME toggle uses `KeyDefinition.AccessibleName`). The close
and password reveal buttons do the same. IME candidate and paging buttons are touch
targets of at least 44 px and are named “第 N 个候选，文本” / “上一页候选” /
“下一页候选”; the candidate bar itself is named “候选词”. The editor
surface is custom-drawn, so screen readers get the key names and the panel chrome
but not a native text-editing semantic; hosts that need full text semantics should
inject their own `IEditorSurface`.

## Testing

Headless tests use `Avalonia.Headless.XUnit` with a custom app builder:

```csharp
[assembly: AvaloniaTestApplication(typeof(TestAppBuilder))]

public static class TestAppBuilder
{
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<TestApp>()
            .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false })
            .UseSkia();
}
```

The test project drives pointer events (`window.MouseDown` / `MouseUp`), captures
rendered frames and asserts session/state behaviour. See
`tests/TouchKeyboard.Avalonia.Tests`.

## Troubleshooting

| Symptom | Check |
|---|---|
| Panel never opens | Field must be enabled, not read-only and not inside a `Button`; the host must be attached to the same window and be last in the grid. |
| Wrong profile | Explicit `TouchKeyboardInput.Profile` wins; otherwise the control-type rules above apply. |
| Panel too tall/wide | Lower `KeyHeight`, `MaxPanelWidth` or `NumericPanelMaxWidth`. |
| Dark mode ignored | The window's actual theme variant must be dark, and `Options.Theme` must stay `null`. |
| Physical input leaks into the field | Keep `HandlePhysicalKeyboard` enabled so the host handles keys while open. |
| Typed spaces look invisible | Expected in a text buffer; the caret still advances. |
| No 中/英 key or candidate bar | Check `UseIme(...)`, the `text` profile and that a language package is installed. |
| Candidates show boxes | Install a CJK font or set a font fallback chain; see [ime.md](ime.md#故障排查). |
| Candidate bar makes the panel too tall | Lower `KeyHeight`; PhoneHybrid uses the candidate row as its first row and the numeric-only keypad is compact. |
