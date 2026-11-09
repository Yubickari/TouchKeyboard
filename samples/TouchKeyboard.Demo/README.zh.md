# TouchKeyboard 演示程序

[English](README.md) · **简体中文**

TouchKeyboard 的 Avalonia 示例。它把浮层宿主连接到覆盖全部内置输入档位的字段，
并启用可选的拼音引擎。

## 运行

```bash
dotnet run --project samples/TouchKeyboard.Demo
```

示例目标框架是 `net8.0`，引用仓库内的 Avalonia 项目。还原使用标准 NuGet 缓存；
沙箱环境请先把 `NUGET_PACKAGES` 指向可写目录。

## 可以尝试

- 点字段打开键盘；标题行显示字段名和单位。
- 用 **确定** 或 Enter 提交；点 ×、点遮罩或按 Esc 关闭并放弃修改。
- 在全键盘 `AnsiStyle` 和 `PhoneHybrid` 之间切换，并在跟随系统/浅色/深色主题间切换。
- 在外观面板中调整整体/字号/图标缩放、语义色、面板拖动、按键语言
  （自动/中文/English）和键盘字体（平台默认/CJK 优先/等宽/衬线）。
- 打开中文名称字段，输入 `nihao`、`beijing` 或 `woaibeijing`，观察候选栏和组合态。
  空格提交高亮候选，‹ / › 翻页；切到 English 键面后 IME 键显示 CN/EN，按键几何不变。
- 检查档位专用字段：正整数没有正负号和小数点；日期使用对应分隔符；IPv4 每段限制
  0–255。
- 密码、只读和禁用键盘字段用于验证适配器规则；只读和禁用字段不会弹出键盘。

## 项目结构

| 文件 | 用途 |
|---|---|
| `Program.cs` / `App.axaml` | Avalonia 入口和主题。 |
| `MainWindow.axaml` | 字段、宿主浮层和外观控件。 |
| `MainWindow.axaml.cs` | 档位、拼音引擎、语言/字体和外观更新。 |
