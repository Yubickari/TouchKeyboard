# Avalonia 接入

[English](avalonia.md) · **简体中文**

`TouchKeyboard.Avalonia` 把键盘渲染成窗口内浮层，面向 `net8.0` 和 `net10.0` 上的
Avalonia 12.0.5 或更高版本；CI 会同时测试 12.0.5 和 12.1.5。核心包不依赖 UI，
窗口和控件始终由宿主应用完全掌控。

```bash
dotnet add package TouchKeyboard.Avalonia
```

## 最简接入

把宿主控件放在窗口根 Grid 的**最后**，让遮罩盖住整个页面，然后用附加属性标记
字段：

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

自动挂接不需要写 code-behind。`TouchKeyboardHost.AutoAttach` 默认为 `true`，
宿主挂到窗口时会创建 `TouchKeyboardController`。控制器监听窗口上的指针按下，
解析出可编辑目标，打开面板，并在面板关闭后恢复焦点。

下面代码片段中的 `KeyboardHost` 是上面 XAML 里由 `x:Name` 生成的 code-behind
字段，控件类型为 `TouchKeyboardHost`。

### 附加属性

| 属性 | 类型 | 含义 |
|---|---|---|
| `TouchKeyboardInput.Profile` | `string?` | 输入档位 id，例如 `positive-decimal` 或 `date-dot`。未知 id 回退到控件类型的默认档位。 |
| `TouchKeyboardInput.Label` | `string?` | 标题行显示的字段说明。未设置时回退到本地化的“编辑”。 |
| `TouchKeyboardInput.Unit` | `string?` | 标签旁显示的单位，例如 `℃`。 |
| `TouchKeyboardInput.Enabled` | `bool` | 按控件单独开关（默认 `true`）；`false` 会让该控件及其子控件都不弹出键盘。 |

未显式指定档位时，控制器按控件类型解析：`CalendarDatePicker` → 日期档位（自定义
格式含点号时使用 `.`），`Minimum >= 0` 的 `NumericUpDown` → `positive-decimal`，
否则 `decimal`，带 `PasswordChar` 的 `TextBox` → `password`，其他可编辑控件 →
`text`。从 `Button` 内部开始的点击、只读字段和禁用字段都不会打开面板。

## 配置

```csharp
public MainWindow()
{
    InitializeComponent();
    KeyboardHost.Configure(options =>
    {
        options.FullKeyboardLayout = FullKeyboardLayoutKind.PhoneHybrid;
        options.NumericKeypadLayout = NumericKeypadLayoutKind.Numpad;
        options.KeyHeight = 64;            // 0 = 按窗口高度取 56/64/72
        options.MaxPanelWidth = 1100;
        options.NumericPanelMaxWidth = 640;
        options.Culture = "zh-CN";         // 内置 zh-CN / en-US
        options.HandlePhysicalKeyboard = true;
    });
}
```

| 选项 | 默认值 | 说明 |
|---|---|---|
| `Enabled` | `true` | 全局开关；为 `false` 时控制器不会打开面板。 |
| `Theme` | `null` | `null` 跟随窗口实际的明暗变体；设为 `TouchKeyboardTheme.Light`/`.Dark`（或自定义实例）可固定主题。 |
| `FullKeyboardLayout` | `AnsiStyle` | `AnsiStyle` 或 `PhoneHybrid`。 |
| `NumericKeypadLayout` | `Numpad` | `Numpad` 是当前启用的形态；`Sequence` 保留给自定义场景。 |
| `MaxPanelWidth` | `1280` | 面板宽度上限，单位为逻辑像素；会乘以 `Style.Scale`。 |
| `NumericPanelMaxWidth` | `720` | 数字键盘区域的宽度上限；会乘以 `Style.Scale`。 |
| `KeyHeight` | `0` | 基础键高；`0` 表示按窗口高度取 56/64/72。会乘以 `Style.Scale`，最小 44 px。 |
| `Style` | `Scale=1` | 尺寸、配色和字体的轻量配置入口；见 [customization.md](customization.md)。 |
| `Style.FontFamily` | `null` | 覆盖按键、标题行、编辑面和候选文字的字体；优先于主题字体。 |
| `EnablePanelDrag` | `true` | 长按标题行可在窗口内拖动面板。 |
| `HandlePhysicalKeyboard` | `true` | 面板打开时：Esc 关闭，Enter 确认，退格/方向键/上下键编辑，文本直接插入。 |
| `SuppressSystemKeyboard` | `true` | 预留项；默认宿主不使用文本输入控件，而是把焦点移到自绘宿主上，从而抑制平台软键盘（OSK）。 |
| `KeyLanguage` | `Auto` | 内置语言，用于键面文案、标题行和校验消息。`Auto` 先跟随 `Culture`，再跟随当前 UI culture；`Chinese` = zh-CN；`English` = en-US。 |
| `Culture` | `null` | `KeyLanguage` 为 `Auto` 时使用的 culture 名称；`null` 跟随当前 UI culture。 |
| `Localizer` | `null` | 自定义 `ITouchKeyboardLocalizer`；优先于 `KeyLanguage` 和 `Culture`。 |
| `EditorSurfaceFactory` | `null` | 自定义 `IEditorSurface` 的工厂；默认使用自绘的 `TouchKeyboardEditor`。 |
| `Ime` | 引擎为 `null` | 可选的托管 IME 配置；见 [ime.md](ime.md)。 |

## 可选中文输入

安装可选语言包，再用一行代码启用：

```bash
dotnet add package TouchKeyboard.Ime.Pinyin
```

```csharp
using TouchKeyboard.Ime.Pinyin;

KeyboardHost.Configure(options => options.UseIme(new PinyinImeEngine()));
```

之后 `text` 字段会多出 IME 键（中文键面为 中/英，英文键面为 `CN`/`EN`）和候选栏
（AnsiStyle 为 44 px；PhoneHybrid 下它是五行的第一行，高度等于键高）。PhoneHybrid
符号页的清空槽位会变成这个 IME 切换键，有映射的符号键帽跟随当前语言（例如中文模式
`.`→`。`；命令仍插入 ASCII 源字符）。数字、日期、密码和 ASCII 档位都不启用 IME。没有引擎时既没有 IME 键也没有候选栏，空格占据
原 IME 键位（AnsiStyle 7.0 单位，PhoneHybrid 3.5）；配置了引擎且档位允许 IME 时，
空格宽度回到 5.5/2.5，英文模式下该键仍然存在，因此来回切换不会改变宽度。候选栏
从不滚动：它只显示能完整放下的候选文字（最多九个），并据此调整 Core 的每页候选数；
用 56 px 的切页按键翻页。基础包 `TouchKeyboard.Avalonia` 从不引用语言包。见
[ime.md](ime.md) 和 [ADR 0004](adr/0004-managed-pinyin-ime.md)。

## 主题

浮层会根据宿主窗口的 `ActualThemeVariant` 解析出 `TouchKeyboardTheme.Light` 或
`.Dark`，并在变体变化时重新应用，因此深色宿主会自动得到深色键盘。关闭/确认键的
对比度来自 `DangerKeyForeground` 和 `PrimaryKeyForeground`。

```csharp
KeyboardHost.Configure(options =>
{
    // 默认自动跟随；这里改为固定预设：
    options.Theme = TouchKeyboardTheme.Dark;

    // 也可以基于内置预设派生自定义主题：
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

按压反馈由叠加层（`PressedOverlay`）和轻微缩放（`PressedScale`）组成；所有按键
都没有悬停态。`KeyLabelOffset` 会把标签略微下移，以补偿字体度量差异。

只需少量调整时可以使用 `options.Style`（`Scale`、`FontScale`、`IconScale`、
`FontFamily`、`AccentColor`、`ConfirmColor`、`ConfirmTextColor`、
`SelectedCandidateColor`、`SelectedCandidateTextColor`、`PanelColor`、`KeyColor`、
`TextColor`）。它叠加在解析后的主题之上，默认值保持不变；需要控制每个画刷和
度量时，完整的 `TouchKeyboardOptions.Theme` 仍然可用。见
[customization.md](customization.md)。

### 语言与字体

`TouchKeyboardTheme.FontFamily` 默认为 `TouchKeyboardFonts.Default`，即一条平台
相关的回退链。`options.Style.FontFamily` 可以覆盖它，同时保留主题的其余部分：

```csharp
using Avalonia.Media;

KeyboardHost.Configure(options =>
{
    // 全英文宿主：键面文案和标题行都用英文。
    options.KeyLanguage = KeyboardLanguage.English;

    // 可选：品牌字体、已知的 CJK 字体，或等宽键帽字体。
    options.Style.FontFamily = new FontFamily("Noto Sans CJK SC");
});
```

| 平台 | 默认回退链 |
|---|---|
| Windows | `Segoe UI, Microsoft YaHei UI, Microsoft YaHei` |
| macOS | `Helvetica Neue, PingFang SC, Hiragino Sans GB` |
| Linux | `Noto Sans, Noto Sans CJK SC, WenQuanYi Micro Hei, DejaVu Sans` |

该字体族作用于键帽、标题/单位/错误行、自绘编辑面、候选序号与候选文字、候选提示
和翻页图标。自定义 `IEditorSurface` 使用自己的字体。这些包不随附字体文件；要显示
中文，机器上必须安装 CJK 字体。

`KeyboardLanguage` 只改变具名按键文案、标题行和校验消息，不改变字母键位几何。英文键面使用
`Confirm`、`Clear`、`Symbols`、`Space`、`Enter` 以及 `CN`/`EN`；中文键面使用
确定、清空、符号、空格、回车 以及 中/英。`KeyLanguage = Auto` 先跟随 `Culture`，
再跟随 `CurrentUICulture`。自定义语言以自定义 `Localizer` 为准；见
[本地化](#本地化)。

### 面板拖动

长按标题行（约 420 ms）后拖动。面板始终被限制在当前窗口内；如果面板本来就铺满
窗口，可移动的空间就很小或没有。位置在宿主生命周期内保持不变，并在窗口缩放时
重新限制在窗口内。`KeyboardHost.ResetPanelPosition()` 可恢复默认的底部居中位置。
设置 `options.EnablePanelDrag = false` 可关闭拖动；关闭按钮仍保持正常的点按行为。

可选 IME 还会增加 `CandidateBarHeight`（默认 44；PhoneHybrid 首行模式下等于
键高）、`CandidateBackground`、`CandidateSelectedBackground`、`CandidateText`、
`CandidateSelectedText`、`CandidateMutedText`、`CandidateCornerRadius`、
`CandidateFontSize`、`CompositionText`、`CompositionUnderline` 和
`CompositionUnderlineThickness`。明暗两套预设都带有对应取值。组合、翻页或切换
中/英时，候选栏保持固定高度。

## 本地化

`ITouchKeyboardLocalizer` 有两项职责：解析按键文案的资源键，以及格式化核心的
校验错误。

```csharp
KeyboardHost.Configure(options => options.Localizer = new MyLocalizer());
```

内置 culture 为 `zh-CN` 和 `en-US`。核心校验从不返回整句，只返回
`InputValidationError` 错误码，由本地化器转成文案。要新增语言，实现该接口，并
映射 [profiles.zh.md](profiles.zh.md#校验错误) 中列出的错误码。

`Options.KeyLanguage` 无需 culture 字符串即可选择内置本地化器：`English` 和
`Chinese` 优先于 `Culture`，自定义 `Localizer` 又优先于这两者。`Auto` 保持基于
culture 的行为。例如，在中文 Windows 上的全英文宿主设置
`options.KeyLanguage = KeyboardLanguage.English;`，键面文案和校验消息都会变成英文，
IME 键显示 `CN`/`EN` 而不是 中/英。

IME 还会增加以下键：`key.ime.active`/`key.ime.inactive`、
`key.ime.active.name`/`key.ime.inactive.name`、`ime.candidates`、`ime.previous`、
`ime.next`、`ime.no-candidate`、`ime.hint.chinese`、`ime.hint.english`、
`ime.candidate.name`、`ime.page` 和 `ime.composing`。自定义本地化器应补齐这些键；
缺少某个键时 `Get` 返回资源键本身。

## 自定义编辑面

默认编辑面是 `TouchKeyboardEditor`，一个自绘的单行控件，用来避开平台的文本输入。
宿主可以注入自己的控件：

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

把真正可编辑的控件当作编辑面，可能会把平台软键盘/IME 带回来——这正是编辑面决策
里描述的取舍。自定义编辑面不必绘制 IME 组合态；但候选栏不再重复显示原始拼音串，
如果编辑面需要展示组合输入，就应自行渲染。`IEditorSurface` 契约没有新增成员。

## 自定义适配器

适配器负责衔接控件与会话。内置适配器覆盖 `TextBox`、`NumericUpDown` 和
`CalendarDatePicker`。自定义控件需要实现 `IEditorAdapter`，并在启动时注册一次
工厂：

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

## 自定义档位

继承 `InputProfile` 并注册：

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

然后通过 `tk:TouchKeyboardInput.Profile="serial-number"` 使用。契约和内置档位集合见
[profiles.zh.md](profiles.zh.md)。

## 无障碍

按键基于 `Border` 而不是按钮，普通 `Border` 会被自动化树当作非控件元素上报。因此
`TouchKey` 自带自动化 peer，类型为 `AutomationControlType.Button`，并带有本地化的
可访问名称（图标键使用其标签资源键，IME 切换键使用
`KeyDefinition.AccessibleName`）。关闭按钮和密码显示按钮也做了同样的处理。IME
候选键和翻页键都是至少 44 px 的触摸目标，名称分别为“第 N 个候选，文本”、
“上一页候选”、“下一页候选”；候选栏本身的名称是“候选词”。编辑面为自绘，屏幕
阅读器能读到按键名称和面板周边控件，但拿不到原生的文本编辑语义；需要完整文本
语义的宿主应注入自己的 `IEditorSurface`。

## 测试

无头测试使用 `Avalonia.Headless.XUnit` 和自定义 app builder：

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

测试项目会驱动指针事件（`window.MouseDown` / `MouseUp`），抓取渲染帧并断言会话/
状态行为。见 `tests/TouchKeyboard.Avalonia.Tests`。

## 排错

| 现象 | 检查 |
|---|---|
| 面板始终不打开 | 字段必须启用、非只读，且不在 `Button` 内；宿主必须挂到同一个窗口，并位于 Grid 的最后。 |
| 档位不对 | 显式 `TouchKeyboardInput.Profile` 优先；否则按上面的控件类型规则解析。 |
| 面板太高/太宽 | 调小 `KeyHeight`、`MaxPanelWidth` 或 `NumericPanelMaxWidth`。 |
| 深色模式未生效 | 窗口实际的明暗变体必须是深色，且 `Options.Theme` 必须保持 `null`。 |
| 物理键盘输入漏进字段 | 保持 `HandlePhysicalKeyboard` 开启，让宿主在面板打开时接管按键。 |
| 输入的空格看起来不可见 | 这是文本缓冲中的正常表现；光标仍会前进。 |
| 没有 `CN`/`EN`（中/英）键或候选栏 | 检查 `UseIme(...)`、`text` 档位，以及是否安装了语言包。 |
| 中文显示成方框 | 安装 CJK 字体，或用覆盖中文的字体设置 `options.Style.FontFamily`；平台默认值已经会尝试 Windows/macOS/Linux 的 CJK 字体。见 [ime.md](ime.md#故障排查)。 |
| 按键文案语言不对 | 设置 `options.KeyLanguage`（`Auto`/`Chinese`/`English`）；自定义 `Localizer` 会覆盖它。 |
| 只有部分文字不跟随字体 | 自定义 `IEditorSurface` 使用自己的字体；请在那个控件上设置字体。 |
| 候选栏把面板撑得太高 | 调小 `KeyHeight`；PhoneHybrid 把候选栏当作第一行，纯数字键盘本身则很紧凑。 |
