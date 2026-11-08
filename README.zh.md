# TouchKeyboard

[English](README.md) · **简体中文**

面向小尺寸工控触摸屏的内置软键盘。输入逻辑是纯 .NET Standard 2.0 后端，
Avalonia 是第一个前端，未来计划加入 WPF。

> **状态：`0.2.0` 实现完成（2026-11-08）。** 核心包含十个输入档位、严格 ANSI 与
> 手机式全键盘、5×4 Numpad 网格、无 IME 空格基线、不可变布局、会话/校验和可选
> 托管 IME 组合态；Avalonia 已有浮层宿主、控制器、自绘编辑面、中/英键、固定候选
> 栏、组合态内联绘制、矢量按键图标、自动明暗主题、触屏按压反馈、内置适配器、
> 主题和 zh-CN/en-US 本地化。可选的 `TouchKeyboard.Ime.Pinyin` 与 `.Extended`
> 提供托管拼音引擎和词库。`0.1.0` 不再发布；`0.2.0` 是首个发布目标。真机验证尚未
> 完成，请勿把当前构建当作已通过生产验证。

## 目标

- 8–10 寸小屏和工控面板：大按键、不依赖系统 OSK、可整体禁用。
- 文本、整数、小数、日期、IP 等字段只显示真正需要的按键；正整数不出现
  正负号和小数点。
- 输入规则与 UI 解耦，可在无 UI 环境单测，方便未来加 WPF 前端。

## 包

| 包 | 目标框架 | 职责 |
|---|---|---|
| `TouchKeyboard.Core` | `netstandard2.0` | 模式、档位、布局、会话、校验、IME 组合态 |
| `TouchKeyboard.Avalonia` | `net8.0;net10.0` | Avalonia 宿主、按键、候选栏、适配器、主题 |
| `TouchKeyboard.Ime.Pinyin` | `netstandard2.0` | 可选托管拼音引擎 + 常用词库（约 2 万词） |
| `TouchKeyboard.Ime.Pinyin.Extended` | `netstandard2.0` | 可选扩展词库（约 10 万词） |
| `TouchKeyboard.Wpf` | 未来 | WPF 前端，复用核心 |

两个主模式：

1. **全键盘**：默认是严格 15 单位的 `AnsiStyle`（5 行，Tab/Caps 保留空位；无
   IME 时空格 7.0 单位覆盖原 IME 槽位，有引擎且 `text` 档位时空格 5.5 单位 +
   1.5 单位中/英键）；4 行手机式 `PhoneHybrid`（自带符号页）作为保留的可选形态
   （无 IME 空格 3.5，有 IME 2.5 + 1 单位中/英键）。不含 F 键、小键盘或系统功能键。
2. **数字小键盘**：通用场景与各档位共用的 5×4 `Numpad`；空槽由相邻键合并
   （小数位为空时 `0` 跨两列，右中槽为空时“确定”跨两行），不生成禁用键。

关闭面板 = 放弃修改；只有“确定”/Enter 才提交。标题行显示字段描述和单位。

![严格 ANSI 全键盘](docs/images/keyboard-full.png)

![档位专用数字键盘](docs/images/keyboard-numeric.png)

![暗色主题](docs/images/keyboard-full-dark.png)

![中文拼音输入与候选栏](docs/images/keyboard-ime-composition.png)

*1024×700 测试环境的 Headless Skia 渲染。浮层默认跟随窗口主题变体，暗色壳自动
使用暗色预设；全部布局、符号页、Numpad 槽位表与主题度量见
[docs/layouts.md](docs/layouts.md)。*

## 当前已完成

- `InputProfile`：文本、密码、ASCII 标识、有符号/正整数、有符号/正小数、
  用 `/` 或 `.` 的日期、IPv4；只有 `text` 的 `AllowsIme` 为 true。
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
- 可选 IME：Core 提供 `IImeEngine`、`ImeCandidate`、`ImeCompositionState`；
  支持中文组合、候选翻页/选中、空格/1–9/回车提交、全角标点；
  `ime.toggle` 占据全键盘槽位。
- Avalonia：带遮罩的 `TouchKeyboardHost`（按情境调整高度的键盘区 + 候选栏；PhoneHybrid 首行候选栏高度等于键高）、
  自动挂载的控制器、自绘 `TouchKeyboardEditor`（组合文本 + 下划线、光标跟随尾部
  空格）、指针捕获 + 长按重复 + 矢量图标的 `TouchKey`、`KeyboardLayoutView`、
  `IEditorSurface` 注入、TextBox/NumericUpDown/CalendarDatePicker 适配器、自定义
  适配器注册表、跟随窗口主题变体的明暗主题、zh-CN/en-US 本地化，以及无悬停态的
  触屏式标题栏按钮。
- 紧凑外观 API：`options.Style.Scale/FontScale/IconScale`，确定键、候选选中、面板、
  按键和文字的语义色；`options.EnablePanelDrag` 支持长按标题行在窗口内拖动面板。
- 每个目标框架 135 个核心测试 + 40 个拼音/扩展测试 + 48 个 Avalonia Headless
  测试（共 446 次执行）。

尚未实现：WPF，以及真机验证（Windows/Linux/macOS 触摸、CJK 字体回退、DPI、
OSK 抑制）。`0.1.0` 不再发布；`0.2.0` 是首个发布目标。

## 中文输入（可选）

安装可选拼音包并加一行配置：

```bash
dotnet add package TouchKeyboard.Avalonia --version 0.2.0
dotnet add package TouchKeyboard.Ime.Pinyin --version 0.2.0
```

```csharp
using TouchKeyboard.Avalonia;
using TouchKeyboard.Ime.Pinyin;

KeyboardHost.Configure(options => options.UseIme(new PinyinImeEngine()));
```

`text` 档位自动出现中/英键和固定候选栏；数字、日期、密码、ASCII 档位行为不变；
不装语言包时与无 IME 状态完全一致（无 IME 键/候选栏，Space 7.0/3.5）。扩展包
只换引擎类型：

```csharp
using TouchKeyboard.Ime.Pinyin.Extended;

KeyboardHost.Configure(options => options.UseIme(new ExtendedPinyinImeEngine()));
```

引擎是纯托管、词典驱动，不接系统 IME，也不做按键注入。选项、交互规则、
主题/本地化/无障碍、包体积、数据来源与排错见 [docs/ime.md](docs/ime.md)。

## 仓库结构

```text
src/TouchKeyboard.Core/             纯后端
src/TouchKeyboard.Avalonia/         Avalonia 前端
src/TouchKeyboard.Ime.Pinyin/       可选拼音引擎 + 常用词库
src/TouchKeyboard.Ime.Pinyin.Extended/ 可选扩展词库
samples/TouchKeyboard.Demo/         演示程序（含中文字段）
tests/TouchKeyboard.Core.Tests/
tests/TouchKeyboard.Ime.Pinyin.Tests/
tests/TouchKeyboard.Avalonia.Tests/
tools/build-pinyin-dict.py          可复现的词典构建脚本
assets/icon.png                     包图标
docs/architecture.md                分层、模型与数据流
docs/avalonia.md                    接入、选项、主题、适配器
docs/ime.md                         中文输入、语言包与许可
docs/customization.md               尺寸、语义色与面板拖动
docs/public-api.md                  公共 API 清单与扩展点
docs/profiles.md                    输入档位、校验与数字键盘槽位
docs/layouts.md                     布局参考
docs/roadmap.md                     发布状态与版本计划
docs/release.md                     NuGet 发布步骤
docs/wpf-porting.md                 WPF 移植说明
docs/design/decisions.md            已确认决策
docs/adr/                           架构决策记录
docs/testing/device-checklist.md    真机验收清单
docs/images/                        渲染预览
build/                              preflight、smoke 与 hygiene 脚本
```

## 构建

需要 .NET 8 与 .NET 10 SDK（AOT smoke 另需 `clang`/`zlib`）。

```bash
dotnet restore TouchKeyboard.sln
dotnet build TouchKeyboard.sln -c Release
dotnet test tests/TouchKeyboard.Core.Tests/TouchKeyboard.Core.Tests.csproj -c Release
dotnet test tests/TouchKeyboard.Ime.Pinyin.Tests/TouchKeyboard.Ime.Pinyin.Tests.csproj -c Release
dotnet test tests/TouchKeyboard.Avalonia.Tests/TouchKeyboard.Avalonia.Tests.csproj -c Release
bash build/preflight.sh
bash build/smoke-pack.sh
```

运行 Demo：

```bash
dotnet run --project samples/TouchKeyboard.Demo
```

Demo 顶部可以切换全键盘样式（AnsiStyle / PhoneHybrid）与界面主题
（跟随系统 / 浅色 / 深色），并提供可折叠测试面板中的整体/字号/图标缩放滑杆、强调色/确定键/
候选选中/面板/普通按键颜色选择、长按拖动开关和复位按钮，以及一个接入拼音引擎
的中文字段。可用中文字段检查候选栏和按压反馈，用 PhoneHybrid + 符号/123 检查
高度模型。

还原使用标准的 NuGet 用户缓存。受限环境/沙箱中请在还原前把 `NUGET_PACKAGES`
指向可写目录；smoke 脚本默认使用仓库内的 `.nuget/packages`。

## 文档

- [架构](docs/architecture.md)：分层、核心模型、数据流与扩展点。
- [Avalonia 接入](docs/avalonia.md)：安装、选项、主题、适配器与排错。
- [中文输入](docs/ime.md)：可选拼音包、候选栏、许可与体积。
- [外观与拖动](docs/customization.md)：尺寸、字号/图标、语义色与标题行拖动。
- [公共 API](docs/public-api.md)：0.2.0 API 清单、兼容策略与扩展点。
- [输入档位](docs/profiles.md)：内置档位、校验规则与数字键盘槽位。
- [布局参考](docs/layouts.md)：AnsiStyle / PhoneHybrid、符号页、Numpad 网格。
- [路线图](docs/roadmap.md)：发布状态、就绪清单与版本计划。
- [发布流程](docs/release.md)：版本、Trusted Publishing 与发布步骤。
- [WPF 移植说明](docs/wpf-porting.md)：第二个前端需要实现什么。
- [架构决策记录](docs/adr/) 与 [已确认决策](docs/design/decisions.md)。
- [中文输入设计稿](docs/design/2026-11-08-chinese-input-and-language-packs.md)：托管拼音路线、可选语言/文本包与一行接入 API。
- [真机验收清单](docs/testing/device-checklist.md)：0.2.0 真机测试项。

## 已知限制

- 托管拼音只做全拼、分词/整句和常用简拼；不做五笔、手写、模糊音、云输入、
  自学习或繁简转换。
- 候选质量是 best-effort；常用词库约 2 万词，扩展包约 10 万词；golden set 是
  开发目标，不是质量保证。
- 编辑面是单行：没有选择、剪贴板、撤销或多行布局。
- 编辑发生在会话缓冲；只有确认才写回目标控件，关闭总是放弃。
- 不含 F 键、NumLock 小键盘块、Ctrl/Alt/Win 组合或系统功能键。
- WPF 尚未实现。
- 真机行为（CJK 字体、OSK 抑制、DPI、1024×600、词典加载时间/内存）仍需按
  清单记录，之后才能称 `0.2.0` 可现场使用。

## 许可证

MIT，见 [LICENSE](LICENSE)。可选语言/文本数据只使用宽松或署名许可
（CC0、MIT、BSD、Apache-2.0、Unicode License 或 CC-BY 4.0），并在 `NOTICE`
记录来源；不随包分发 CC-BY-SA、GPL/AGPL、专有或来源不明的数据。拼音词库使用
Unihan `kMandarin` 字音（Unicode License V3）和 jieba 词表/词频（MIT）；每个
语言包携带 `NOTICE`，列出源、快照哈希和完整许可文本。项目不附加商业限制：
任何人都可以免费使用、修改和再分发。

软件和可选数据按现状提供；不承诺候选质量或稳定性，维护为 best-effort。
