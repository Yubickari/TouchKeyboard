# TouchKeyboard.Core

[English](README.md) · **简体中文**

本包是 [TouchKeyboard](https://github.com/Yubickari/TouchKeyboard) 的纯 .NET Standard 2.0 核心。
TouchKeyboard 是面向小尺寸工控触摸屏、以触屏操作为先的嵌入式软键盘。

本包提供无 UI 的输入模型：输入档位、校验、不可变的键盘布局、编辑会话、
IME 组合契约以及与区域设置无关的校验错误。它不依赖 UI、平台或任何包，
因此前端可以在任意受支持的 .NET 目标上复用它。

## 安装

```bash
dotnet add package TouchKeyboard.Core
```

## 包含内容

- `InputProfile` 和 `InputProfileRegistry`：十个内置档位（`text`、`password`、
  `ascii-token`、`integer`、`positive-integer`、`decimal`、
  `positive-decimal`、`date-slash`、`date-dot`、`ip-address`）以及自定义档位。
- `KeyboardLayout`、`KeyboardRow`、`KeyboardGridLayout`、`KeyboardGridCell`、
  `KeyDefinition`、`KeyLabel`、`KeyStyle`、`KeyGlyph` 和 `KeyCommand`。
- `KeyboardSession`、`KeyboardSessionOptions` 和不可变的 `KeyboardState` 快照，
  支持提交/取消与校验。
- `NumericKeypadOptions`、`NumpadSlots` 和 `NumpadSlotKey`，用于数字小键盘场景。
- `IImeEngine`、`ImeCandidate` 和 `ImeCompositionState`，用于可选、语言中立的
  IME 组合。

## 最简会话

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
    // 宿主将 `text` 写回自己的控件。
}
else
{
    // `error` 是与区域设置无关的 InputValidationError。
}
```

## 文档

- [架构](https://github.com/Yubickari/TouchKeyboard/blob/main/docs/architecture.md)
- [输入档位](https://github.com/Yubickari/TouchKeyboard/blob/main/docs/profiles.zh.md)
- [布局参考](https://github.com/Yubickari/TouchKeyboard/blob/main/docs/layouts.zh.md)
- [公共 API](https://github.com/Yubickari/TouchKeyboard/blob/main/docs/public-api.md)

## 许可证

MIT 许可，见 [LICENSE](https://github.com/Yubickari/TouchKeyboard/blob/main/LICENSE)。
