using System.Reflection;
using TouchKeyboard.Ime.Pinyin;

namespace TouchKeyboard.Ime.Pinyin.Extended;

/// <summary>
/// The extended pinyin engine: it reuses <see cref="PinyinImeEngine"/> and the
/// common loader, and ships a larger lexicon of about 100k frequent words and
/// phrases. Activation is still one line:
/// <c>options.UseIme(new ExtendedPinyinImeEngine())</c>.
/// </summary>
public sealed class ExtendedPinyinImeEngine : PinyinImeEngine
{
    internal const string ExtendedDictionaryResource =
        "TouchKeyboard.Ime.Pinyin.Extended.Resources.pinyin-extended.bin";

    /// <summary>Creates the engine with the extended lexicon.</summary>
    public ExtendedPinyinImeEngine()
        : base(typeof(ExtendedPinyinImeEngine).GetTypeInfo().Assembly, ExtendedDictionaryResource)
    {
    }
}
