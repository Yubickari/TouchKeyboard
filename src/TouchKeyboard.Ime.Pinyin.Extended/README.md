# TouchKeyboard.Ime.Pinyin.Extended

Optional extended pinyin lexicon for [TouchKeyboard](https://github.com/Yubickari/TouchKeyboard).

This package reuses `TouchKeyboard.Ime.Pinyin` and adds a larger lexicon of about
100,000 frequent words and phrases. It is a compile-time NuGet package, not a
runtime plugin: install it, keep the same one-line activation, and only the
engine type changes.

## Install and activate

```bash
dotnet add package TouchKeyboard.Avalonia --version 0.2.0
dotnet add package TouchKeyboard.Ime.Pinyin.Extended --version 0.2.0
```

```csharp
using TouchKeyboard.Avalonia;
using TouchKeyboard.Ime.Pinyin.Extended;

KeyboardHost.Configure(options =>
{
    options.UseIme(new ExtendedPinyinImeEngine());
});
```

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

For configuration options and troubleshooting see `docs/ime.md` in the
repository.
