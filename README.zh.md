# TouchKeyboard

[English](README.md) · **简体中文**

面向小尺寸工控触摸屏的内置软键盘。输入逻辑是纯 .NET Standard 2.0 后端，
Avalonia 是第一个前端，未来计划加入 WPF。

> **状态：布局改版与视觉细化已实现（2026-11-07）。** 核心包含十个输入档位、
> 严格 ANSI 与手机式全键盘、5×4 Numpad 网格、不可变布局、会话与校验；Avalonia
> 已有浮层宿主、控制器、自绘编辑面、矢量按键图标、自动明暗主题、触屏按压反馈、
> 内置适配器、主题和 zh-CN/en-US 本地化。P5 发布与剩余硬化项仍待完成，
> 请勿在生产环境使用当前包。

## 目标

- 8–10 寸小屏和工控面板：大按键、不依赖系统 OSK、可整体禁用。
- 文本、整数、小数、日期、IP 等字段只显示真正需要的按键；正整数不出现
  正负号和小数点。
- 输入规则与 UI 解耦，可在无 UI 环境单测，方便未来加 WPF 前端。

## 计划形态

| 包 | 目标框架 | 职责 |
|---|---|---|
| `TouchKeyboard.Core` | `netstandard2.0` | 模式、档位、布局、会话、校验 |
| `TouchKeyboard.Avalonia` | `net8.0;net10.0` | Avalonia 宿主、按键、适配器、主题 |
| `TouchKeyboard.Wpf` | 未来 | WPF 前端，复用核心 |

两个主模式：

1. **全键盘**：默认是严格 15 单位的 `AnsiStyle`（5 行，Tab/Caps/IME 为无命令
   空位）；4 行手机式 `PhoneHybrid`（自带符号页）作为保留的可选形态。不含 F 键、
   小键盘或系统功能键。
2. **数字小键盘**：通用场景与各档位共用的 5×4 `Numpad`；空槽由相邻键合并
   （小数位为空时 `0` 跨两列，右中槽为空时“确定”跨两行），不生成禁用键。

关闭面板 = 放弃修改；只有“确定”/Enter 才提交。标题行显示字段描述和单位。

![严格 ANSI 全键盘](docs/images/keyboard-full.png)

![档位专用数字键盘](docs/images/keyboard-numeric.png)

![暗色主题](docs/images/keyboard-full-dark.png)

*1024×700 测试环境的 Headless Skia 渲染。浮层默认跟随窗口主题变体，暗色壳自动
使用暗色预设；全部布局、符号页、Numpad 槽位表与主题度量见
[docs/layouts.md](docs/layouts.md)。*

## 当前已完成

- `InputProfile`：文本、密码、ASCII 标识、有符号/正整数、有符号/正小数、
  用 `/` 或 `.` 的日期、IPv4。
- 不可变 `KeyboardLayout` 数据：严格 `AnsiStyle` 与 4 行 `PhoneHybrid` 全键盘、
  PhoneHybrid 符号页、真实网格 + 跨行跨列的 5×4 `Numpad`、按档位生成的特殊
  数字键盘（无用键直接省略），以及保留的 `Sequence` 数字面。
- `KeyGlyph` 语义图标：退格、清空、回车、Shift（空心/实心）、实心三角+光标条的方向箭头、
  三角配 `+`/`−` 的步进图标、空格与密码显隐，由前端画成矢量图标，本地化标签
  保留为无障碍名称。
- `NumpadSlots` 场景槽位：步进键还是 `+`/`−` 字符、左中槽是 `ABC` 还是 `±`、
  底部小数/日期/IP 分隔符。
- `KeyboardSession`：插入、退格、清空、光标、按光标数字增减、正负号切换、
  粘性 Shift、模式切换、全键盘页面切换、提交/取消、最大长度、数值范围校验。
- Avalonia：带遮罩的 `TouchKeyboardHost`（固定 5 行键盘区）、自动挂载的控制器、
  自绘 `TouchKeyboardEditor`（光标跟随尾部空格）、指针捕获 + 长按重复 + 矢量图标的 `TouchKey`、
  `KeyboardLayoutView`、`IEditorSurface` 注入、TextBox/NumericUpDown/
  CalendarDatePicker 适配器、自定义适配器注册表、跟随窗口主题变体的明暗主题、
  zh-CN/en-US 本地化，以及无悬停态的触屏式标题栏按钮。
- 每个目标框架 101 个核心测试 + 23 个 Avalonia Headless 测试（共 248 次执行）。

尚未实现：WPF、NuGet 发布（P5）、中文输入，以及剩余硬化项（真机 DPI/OSK 验证、
无障碍、发布细节）。

## 构建

需要 .NET 8 与 .NET 10 SDK。

```bash
dotnet restore TouchKeyboard.sln
dotnet build TouchKeyboard.sln -c Release
dotnet test tests/TouchKeyboard.Core.Tests/TouchKeyboard.Core.Tests.csproj -c Release
dotnet test tests/TouchKeyboard.Avalonia.Tests/TouchKeyboard.Avalonia.Tests.csproj -c Release
dotnet pack src/TouchKeyboard.Core/TouchKeyboard.Core.csproj -c Release -o artifacts
dotnet pack src/TouchKeyboard.Avalonia/TouchKeyboard.Avalonia.csproj -c Release -o artifacts
```

运行 Demo：

```bash
dotnet run --project samples/TouchKeyboard.Demo
```

Demo 顶部可以切换全键盘样式（AnsiStyle / PhoneHybrid）与界面主题
（跟随系统 / 浅色 / 深色），便于对比两种形态与明暗预设。

还原使用标准的 NuGet 用户缓存。受限环境/沙箱中请在还原前把 `NUGET_PACKAGES`
指向可写目录。

## 设计

已确认决策见 [docs/design/decisions.md](docs/design/decisions.md)，各布局的几何
见 [docs/layouts.md](docs/layouts.md)。详细方案文档在公开文档重写前只保留在本地。

## 许可证

MIT，见 [LICENSE](LICENSE)。
