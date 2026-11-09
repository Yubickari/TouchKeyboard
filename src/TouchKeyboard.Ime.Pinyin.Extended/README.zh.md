# TouchKeyboard.Ime.Pinyin.Extended

[English](README.md) · **简体中文**

[TouchKeyboard](https://github.com/Yubickari/TouchKeyboard) 的可选扩展拼音词库。

本包复用 `TouchKeyboard.Ime.Pinyin`，并增加约 10 万常用词和短语的更大词库。
它是编译时 NuGet 包，而不是运行时插件：安装后保持同样的一行式启用方式，
只有引擎类型需要改。

## 安装与启用

```bash
dotnet add package TouchKeyboard.Avalonia
dotnet add package TouchKeyboard.Ime.Pinyin.Extended
```

```csharp
using TouchKeyboard.Avalonia;
using TouchKeyboard.Ime.Pinyin.Extended;

KeyboardHost.Configure(options =>
{
    options.UseIme(new ExtendedPinyinImeEngine());
});
```

`KeyboardHost` 是宿主 XAML 中为 `TouchKeyboardHost` 生成的 `x:Name`。

`TouchKeyboard.Ime.Pinyin.Extended` 依赖版本匹配的 `TouchKeyboard.Ime.Pinyin` 和
`TouchKeyboard.Core`。不要混用不同次版本。

## 范围与数据

与常用包相同的引擎和行为，词库约 10 万条。代码使用 MIT 许可。
内置词库使用 Unihan `kMandarin` 字音（Unicode License V3）和 jieba 词表及词频（MIT）；
包内携带 `NOTICE`，列出数据来源、快照哈希和完整许可文本。
词组读音和多音字覆盖表由本项目生成并维护。

数据与软件按现状提供，不承诺候选质量、覆盖率、性能或稳定性；
维护采用尽力而为的方式。

配置选项与排错见[中文输入](https://github.com/Yubickari/TouchKeyboard/blob/main/docs/ime.md)。
全英文宿主不需要本包；也可以保留本包，并设置
`options.KeyLanguage = KeyboardLanguage.English`，以使用英文键面文案。
