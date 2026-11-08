# 公共 API 与扩展方式

本页列出对宿主开放的公共 API。完整外观示例见
[customization.md](customization.md)，中文输入见 [ime.md](ime.md)，档位见
[profiles.md](profiles.md)。

## 兼容策略

- 公共 API **只增不改**：新构造/重载/属性方法可以追加，旧签名保留。
- 枚举值只追加，现有值不变。
- 语言包与 Core/Avalonia 同一 minor；扩展包依赖同版本常用包。
- 不承诺内部实现：`ImeComposition`、候选栏内部控件、自动化 Peer、主题派生实现
  都不是公共 API。

## Core（`TouchKeyboard.Core`，`netstandard2.0`）

| 类型 | 用途 |
|---|---|
| `KeyboardMode` / `FullKeyboardLayoutKind` / `FullKeyboardPage` / `NumericKeypadLayoutKind` | 模式、几何、页面和数字面的选择。 |
| `InputProfile` / `NumericInputProfile` / `InputProfiles` / `InputProfileRegistry` | 输入契约、内置档位、注册表。自定义档位继承 `InputProfile` 并注册。 |
| `KeyboardSession` / `KeyboardSessionOptions` / `KeyboardState` | 会话、选项和不可变快照；`State.Ime` 是可选的组合态。 |
| `KeyCommand` / `KeyCommandKind` | UI 无关的按键命令；`ToggleIme` 为新增值。 |
| `KeyDefinition` / `KeyLabel` / `KeyGlyph` / `KeyStyle` | 布局中的键、标签、图标和视觉角色。 |
| `KeyboardLayout` / `KeyboardRow` / `KeyboardGridLayout` / `KeyboardGridCell` / `KeyboardLayouts` | 不可变布局和内置布局工厂。 |
| `NumericKeypadOptions` / `NumpadSlots` / `NumpadSlotKey` | 数字键盘槽位映射。 |
| `InputValidationError` / `InputValidationErrorCode` | 文化中立的校验错误。 |
| `IImeEngine` / `ImeCandidate` / `ImeCompositionState` | 可选 IME 契约、候选和只读组合快照。 |

常用入口：

```csharp
var session = new KeyboardSession(new KeyboardSessionOptions
{
    Text = "42",
    Profile = InputProfiles.PositiveInteger,
    MaxLength = 10
});
```

## Avalonia（`TouchKeyboard.Avalonia`，`net8.0;net10.0`）

| 类型/成员 | 用途 |
|---|---|
| `TouchKeyboardHost` | 浮层宿主；`Configure(...)`、`Open(...)`、`Close()`、`ResetPanelPosition()`。 |
| `TouchKeyboardController` | 窗口级指针控制器；通常由 `AutoAttach` 自动创建。 |
| `TouchKeyboardOptions` | 外观、尺寸、IME、本地化和编辑面配置。 |
| `TouchKeyboardStyle` | 紧凑尺寸/语义色门面：`Scale`、`FontScale`、`IconScale`、`AccentColor`、`ConfirmColor`、`ConfirmTextColor`、`SelectedCandidateColor`、`SelectedCandidateTextColor`、`PanelColor`、`KeyColor`、`TextColor`。 |
| `TouchKeyboardTheme` | 完整画刷/字体/圆角/间距/候选栏度量；`Light`/`Dark` 预设和自定义实例。 |
| `TouchKeyboardImeOptions` | 可选 IME 的引擎、默认中/英、记忆、候选页大小、组合上限和标点映射。 |
| `IEditorAdapter` / `IEditorAdapterFactory` / `TouchKeyboardAdapterRegistry` | 目标控件适配器及自定义注册。 |
| `IEditorSurface` | 替换默认自绘编辑面；不新增成员。 |
| `ITouchKeyboardLocalizer` / `TouchKeyboardLocalizer` | 文案、键名、校验错误和 IME 文案。 |
| `KeyboardLayoutView` / `TouchKey` / `TouchKeyboardEditor` | 公共可复用视图；宿主一般只通过 Host 使用。 |
| `TouchKeyboardInput` | `Profile`/`Label`/`Unit`/`Enabled` 附加属性。 |

## 语言包

| 类型 | 包 |
|---|---|
| `PinyinImeEngine` | `TouchKeyboard.Ime.Pinyin` |
| `ExtendedPinyinImeEngine` | `TouchKeyboard.Ime.Pinyin.Extended` |

接入只有一行：

```csharp
KeyboardHost.Configure(options => options.UseIme(new PinyinImeEngine()));
```

`KeyboardHost` 是 XAML 中 `x:Name` 生成的 code-behind 字段，控件类型是
`TouchKeyboardHost`。

## 开放方式

### 1. 少量外观旋钮（推荐）

```csharp
using Avalonia.Media;

KeyboardHost.Configure(options =>
{
    options.Style.Scale = 0.85;
    options.Style.FontScale = 0.95;
    options.Style.IconScale = 1.10;
    options.Style.AccentColor = Color.Parse("#FF0F766E");
    options.EnablePanelDrag = true;
});
```

### 2. 完整主题

`options.Theme = new TouchKeyboardTheme { ... }` 控制每个画刷和度量；`null` 跟随
窗口明暗。`Style` 会叠加在解析后的主题上。

### 3. 尺寸

`options.KeyHeight`、`options.MaxPanelWidth`、`options.NumericPanelMaxWidth` 是
基础尺寸，最终再乘 `Style.Scale`；键高与触摸目标不小于 44 px。

### 4. 面板拖动

`options.EnablePanelDrag = true`（默认）时，长按标题行约 420 ms 后可拖动；位置被
夹在当前窗口内。`KeyboardHost.ResetPanelPosition()` 复位。

### 5. 自定义档位、适配器、编辑面和 IME

- 新档位：继承 `InputProfile`，`InputProfileRegistry.Register(...)`。
- 新控件：实现 `IEditorAdapter`/`IEditorAdapterFactory`，注册到
  `TouchKeyboardAdapterRegistry`。
- 自有编辑面：实现 `IEditorSurface`，通过
  `TouchKeyboardOptions.EditorSurfaceFactory` 注入。
- 新语言：实现 `IImeEngine` 的独立 `netstandard2.0` 包，通过 `UseIme` 激活；
  不需要反射、程序集扫描或 DI。

## 明确不开放 / 不承诺

- 候选栏内部控件类型、`ImeComposition` 状态机、主题派生内部方法。
- 系统 IME 桥接、按键注入、跨进程输入。
- WPF API（尚未实现）。
- 多语言切换 UI、语言市场、动态插件发现。
- 按键注入所需的平台权限/原生接口。
