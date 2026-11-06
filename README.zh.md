# TouchKeyboard

[English](README.md) · **简体中文**

面向小尺寸工控触摸屏的内置软键盘。输入逻辑是纯 .NET Standard 2.0 后端，
Avalonia 是第一个前端，未来计划加入 WPF。

> **状态：P1 走通骨架。** 解决方案、打包链路和 CI 已存在，但键盘 UI 与输入引擎
> 尚未实现。请勿在生产环境使用当前包。

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
