# TouchKeyboard

[English](README.md) · **简体中文**

面向小尺寸工控触摸屏的内置软键盘。输入逻辑是纯 .NET Standard 2.0 后端，
Avalonia 是第一个前端，未来计划加入 WPF。

> **状态：P2 核心已实现。** .NET Standard 核心已包含十个输入档位、不可变布局、
> 会话状态机与校验，每个目标框架 78 个测试。Avalonia 面板/控制器仍是 P1 占位。
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

1. **全键盘**：现代手机键盘交互 + 标准美式 ANSI 键序和数字行；不含 F 键、
   小键盘或系统功能键。
2. **数字小键盘**：通用场景（6×4 数字块 / 手机 123 页可切换）；特殊场景按
   整数、小数、日期、IP 等档位只生成对应按键。

关闭面板 = 放弃修改；只有“确定”/Enter 才提交。标题行显示字段描述和单位。

## 当前已完成

- `InputProfile`：文本、密码、ASCII 标识、有符号/正整数、有符号/正小数、
  用 `/` 或 `.` 的日期、IPv4。
- 不可变 `KeyboardLayout` 数据：`PhoneHybrid` 全键盘、通用 `Numpad`/`Phone`
  数字面、按档位生成的特殊数字键盘（无关键直接省略）。
- `KeyboardSession`：插入、退格、清空、光标、按光标数字增减、正负号切换、
  粘性 Shift、模式切换、提交/取消、最大长度、数值范围校验。
- 每个目标框架 78 个核心测试（net8.0 与 net10.0）。

尚未实现：Avalonia 面板/控制器/编辑面（P3）、WPF、NuGet 发布（P5）、中文输入。

## 构建

需要 .NET 8 与 .NET 10 SDK。

```bash
dotnet restore TouchKeyboard.sln
dotnet build TouchKeyboard.sln -c Release
dotnet test tests/TouchKeyboard.Core.Tests/TouchKeyboard.Core.Tests.csproj -c Release
dotnet pack src/TouchKeyboard.Core/TouchKeyboard.Core.csproj -c Release -o artifacts
dotnet pack src/TouchKeyboard.Avalonia/TouchKeyboard.Avalonia.csproj -c Release -o artifacts
```

运行 Demo：

```bash
dotnet run --project samples/TouchKeyboard.Demo
```

还原使用标准的 NuGet 用户缓存。受限环境/沙箱中请在还原前把 `NUGET_PACKAGES`
指向可写目录。

## 设计

已确认决策见 [docs/design/decisions.md](docs/design/decisions.md)。详细方案文档
在公开文档重写前只保留在本地。

## 许可证

MIT，见 [LICENSE](LICENSE)。
