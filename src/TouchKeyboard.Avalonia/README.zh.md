# TouchKeyboard.Avalonia

[English](README.md) · **简体中文**

面向小尺寸工控触摸屏的 [Avalonia 12.0.5+](https://avaloniaui.net/) 触屏浮层键盘。
本包是 [TouchKeyboard](https://github.com/Yubickari/TouchKeyboard) 的 Avalonia
前端；输入模型位于 `TouchKeyboard.Core`。

键盘在宿主窗口中打开，用遮罩覆盖页面，编辑单行缓冲，
并通过适配器提交校验后的文本。它不依赖平台的屏幕键盘。

## 安装

```bash
dotnet add package TouchKeyboard.Avalonia
```

## 快速开始

把宿主作为最后一个子元素放入窗口根 Grid，并用附加属性标记字段：

```xml
<Window xmlns="https://github.com/avaloniaui"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:tk="using:TouchKeyboard.Avalonia">
  <Grid>
    <StackPanel Margin="16" Spacing="12">
      <TextBox tk:TouchKeyboardInput.Profile="text"
               tk:TouchKeyboardInput.Label="Name" />

      <NumericUpDown tk:TouchKeyboardInput.Profile="positive-decimal"
                     tk:TouchKeyboardInput.Label="Temperature"
                     tk:TouchKeyboardInput.Unit="℃"
                     Minimum="0" Maximum="100" Value="25" />

      <TextBox tk:TouchKeyboardInput.Profile="password"
               tk:TouchKeyboardInput.Label="Password"
               PasswordChar="*" />
    </StackPanel>

    <tk:TouchKeyboardHost x:Name="KeyboardHost" />
  </Grid>
</Window>
```

```csharp
using Avalonia.Controls;
using TouchKeyboard.Avalonia;
using TouchKeyboard.Core;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        KeyboardHost.Configure(options =>
        {
            options.FullKeyboardLayout = FullKeyboardLayoutKind.AnsiStyle;
            options.KeyHeight = 64;
            options.MaxPanelWidth = 1100;
            options.Culture = "zh-CN";
        });
    }
}
```

`KeyboardHost` 是 `x:Name` 生成的 code-behind 字段；控件类型是
`TouchKeyboardHost`。`AutoAttach` 默认开启。

## 内置集成

- 带遮罩、字段名/单位/错误行、自绘编辑面和固定键盘区的浮层宿主。
- 内置 `TextBox`、`NumericUpDown` 和 `CalendarDatePicker` 的 `IEditorAdapter`
  适配器；也可以注册自定义适配器。
- 自动跟随明暗主题，可用 `TouchKeyboardTheme` 覆盖，并提供紧凑的
  `TouchKeyboardOptions.Style` 外观配置入口，用于缩放、语义色和字体。
- 平台相关的默认字体链（`TouchKeyboardFonts`），以及供宿主覆盖字体的
  `Style.FontFamily`。
- `KeyboardLanguage` 用于切换中文或英文的具名按键文案和界面文字，不改变键位几何。
- 触屏按键支持指针捕获、按压反馈和长按连发。
- 内置 `zh-CN` 和 `en-US` 本地化器；其他区域设置可实现 `ITouchKeyboardLocalizer`。
- 物理键盘处理和焦点恢复。
- 长按标题行拖动面板（`EnablePanelDrag`，默认 `true`）。

## 语言与字体

```csharp
KeyboardHost.Configure(options =>
{
    options.KeyLanguage = KeyboardLanguage.English; // 英文键面文案：Confirm, Clear, Symbols, CN/EN
    options.Style.FontFamily = new FontFamily("Noto Sans CJK SC");
});
```

`Auto`（默认）先跟随 `Culture`，再跟随当前 UI culture。各平台字体链为：Windows
上 `Segoe UI, Microsoft YaHei UI, Microsoft YaHei`，macOS 上 `Helvetica Neue,
PingFang SC, Hiragino Sans GB`，Linux 上 `Noto Sans, Noto Sans CJK SC,
WenQuanYi Micro Hei, DejaVu Sans`。字体文件不随包附带；
显示中文需要机器上已安装 CJK 字体。

## 可选中文输入

```bash
dotnet add package TouchKeyboard.Ime.Pinyin
```

```csharp
using TouchKeyboard.Ime.Pinyin;

KeyboardHost.Configure(options => options.UseIme(new PinyinImeEngine()));
```

`text` 字段会出现 IME 键（中文键面为 中/英，英文键面为 `CN`/`EN`）、固定候选栏和内联组合态。
数字、日期、密码和 ASCII 档位保持无 IME。
未配置引擎时没有 IME 键和候选行，空格保持无 IME 时的加宽宽度。

## 文档

- [Avalonia 接入](https://github.com/Yubickari/TouchKeyboard/blob/main/docs/avalonia.zh.md)
- [输入档位](https://github.com/Yubickari/TouchKeyboard/blob/main/docs/profiles.zh.md)
- [布局参考](https://github.com/Yubickari/TouchKeyboard/blob/main/docs/layouts.zh.md)
- [中文输入](https://github.com/Yubickari/TouchKeyboard/blob/main/docs/ime.md)
- [外观、字体与尺寸](https://github.com/Yubickari/TouchKeyboard/blob/main/docs/customization.md)
- [公共 API](https://github.com/Yubickari/TouchKeyboard/blob/main/docs/public-api.md)

## 许可证

MIT 许可，见 [LICENSE](https://github.com/Yubickari/TouchKeyboard/blob/main/LICENSE)。
