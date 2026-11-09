# TouchKeyboard.Ime.Pinyin.Extended

**English** · [简体中文](https://github.com/Yubickari/TouchKeyboard/blob/main/src/TouchKeyboard.Ime.Pinyin.Extended/README.zh.md)

Optional extended pinyin lexicon for [TouchKeyboard](https://github.com/Yubickari/TouchKeyboard).

This package reuses `TouchKeyboard.Ime.Pinyin` and adds a larger lexicon of about
100,000 frequent words and phrases. It is a compile-time NuGet package, not a
runtime plugin: install it, keep the same one-line activation, and only the
engine type changes.

## Install and activate

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

`KeyboardHost` is the `x:Name` generated for the `TouchKeyboardHost` in the
host's XAML.

`TouchKeyboard.Ime.Pinyin.Extended` depends on the matching
`TouchKeyboard.Ime.Pinyin` and `TouchKeyboard.Core` versions. Do not mix minor
versions.

## Scope and data

Same engine and behaviour as the common pack, with about 100,000 entries. The
code is MIT. The embedded lexicon uses Unihan `kMandarin` readings (Unicode
License V3) and the jieba word list/frequencies (MIT); the package carries a
`NOTICE` with the sources, snapshot hashes and full licence texts. Phrase
readings and the polyphone override table are project-owned.

The data and software are provided as-is. No candidate quality, coverage,
performance or stability guarantee is made; maintenance is best-effort.

For configuration options and troubleshooting see
[Chinese input](https://github.com/Yubickari/TouchKeyboard/blob/main/docs/ime.md).
An all-English host does not need this package; it can also keep the package and
set `options.KeyLanguage = KeyboardLanguage.English` for English key legends.
