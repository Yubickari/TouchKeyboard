# TouchKeyboard.Ime.Pinyin

[English](README.md) · **简体中文**

[TouchKeyboard](https://github.com/Yubickari/TouchKeyboard) 的可选托管拼音输入。

本包为 `text` 字段提供 `PinyinImeEngine` 和一份紧凑的常用词库（约 2 万词和短语）。
它只依赖 `TouchKeyboard.Core`（`netstandard2.0`），不依赖 Avalonia、原生库或平台。
UI（IME 键、候选栏、内联组合态）位于 `TouchKeyboard.Avalonia`，且语言中立。

## 安装与启用

```bash
dotnet add package TouchKeyboard.Avalonia
dotnet add package TouchKeyboard.Ime.Pinyin
```

```csharp
using TouchKeyboard.Avalonia;
using TouchKeyboard.Ime.Pinyin;

KeyboardHost.Configure(options =>
{
    options.UseIme(new PinyinImeEngine());
});
```

`KeyboardHost` 是宿主 XAML 中为 `TouchKeyboardHost` 生成的 `x:Name`。

这就是全部接入步骤。`text` 字段会显示 IME 键（中文键面为 中/英，英文键面为 `CN`/`EN`）和固定候选栏。
数字、日期、密码和 ASCII 标识档位保持无 IME。
未配置引擎时，键盘没有 IME 键和候选行，空格占据原 IME 槽位（AnsiStyle 7.0 单位，PhoneHybrid 3.5）。

## 功能范围

- 全拼、分词/整句以及常用简拼（`nihao` → 你好，`bj` → 北京，`woaibeijing` →
  我爱北京）。
- 中文输入启用时，常用标点会映射为全角形式。
- 不支持五笔、手写、模糊音、云输入、用户自学习或繁简转换。
- 引擎在构造后不可变且线程安全；创建一个实例并在各会话间共享即可。
  词库在首次查询时延迟加载。

## 数据与许可

代码使用 MIT 许可。内置词库使用 Unihan `kMandarin` 字音（Unicode License V3）和
jieba 词表及词频（MIT）。包内携带 `NOTICE`，列出数据来源、快照哈希和完整许可文本。
词组读音和多音字覆盖表由本项目生成并维护。

数据与软件按现状提供，不承诺候选质量、覆盖率、性能或稳定性；
维护采用尽力而为的方式。

配置选项与排错见[中文输入](https://github.com/Yubickari/TouchKeyboard/blob/main/docs/ime.md)。
全英文宿主不需要本包；也可以保留本包，并设置
`options.KeyLanguage = KeyboardLanguage.English`，以使用英文键面文案。
