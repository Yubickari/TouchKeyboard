# TouchKeyboard.Ime.Pinyin

Optional managed pinyin input for [TouchKeyboard](https://github.com/Yubickari/TouchKeyboard).

This package adds `PinyinImeEngine` and a compact common lexicon (about 20,000
words and phrases) for `text` fields. It depends only on
`TouchKeyboard.Core` (`netstandard2.0`) and has no Avalonia, native or platform
dependency. The UI (IME key, candidate bar, inline composition) lives in
`TouchKeyboard.Avalonia` and is language-neutral.

## Install and activate

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

`KeyboardHost` is the `x:Name` generated for the `TouchKeyboardHost` in the
host's XAML.

That is the whole integration. `text` fields show a 中/英 key and a fixed
candidate bar. Numeric, date, password and ASCII-token profiles stay IME-free.
With no engine configured, the keyboard has no IME key or candidate row and
Space fills the former IME slot (AnsiStyle 7.0 units, PhoneHybrid 3.5).

## Scope

- Full pinyin, word/sentence segmentation and common initial-letter
  abbreviations (`nihao` → 你好, `bj` → 北京, `woaibeijing` → 我爱北京).
- Common punctuation maps to full-width forms when Chinese input is active.
- No Wubi, handwriting, fuzzy pinyin, cloud input, user learning or
  Traditional/Simplified conversion.
- The engine is immutable and thread-safe after construction; create one
  instance and share it across sessions. The lexicon is loaded lazily on the
  first query.

## Data and licence

The code is MIT. The embedded lexicon uses Unihan `kMandarin` readings (Unicode
License V3) and the jieba word list/frequencies (MIT). The package carries a
`NOTICE` with the sources, snapshot hashes and full licence texts. Phrase
readings and the polyphone override table are project-owned.

The data and software are provided as-is. No candidate quality, coverage,
performance or stability guarantee is made; maintenance is best-effort.

For configuration options and troubleshooting see
[Chinese input](https://github.com/Yubickari/TouchKeyboard/blob/main/docs/ime.md).
