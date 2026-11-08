using System;
using System.IO;
using System.Linq;
using TouchKeyboard.Core;
using TouchKeyboard.Ime.Pinyin.Extended;
using Xunit;

namespace TouchKeyboard.Ime.Pinyin.Tests;

public sealed class ExtendedPinyinImeEngineTests
{
    [Fact]
    public void Extended_engine_keeps_the_common_contract()
    {
        var engine = new ExtendedPinyinImeEngine();
        Assert.Equal("zh-Hans", engine.LanguageTag);

        Assert.True(engine.TryQuery("nihao", 10, out var candidates));
        Assert.Equal("你好", candidates[0].Text);
    }

    [Fact]
    public void Extended_engine_finds_words_outside_the_common_lexicon()
    {
        var engine = new ExtendedPinyinImeEngine();

        var cases = new (string Input, string Expected)[]
        {
            ("houtougu", "猴头菇"),
            ("zhifangsuan", "脂肪酸"),
            ("maibo", "脉搏"),
            ("jiaogen", "脚跟"),
            ("yaofang", "药房")
        };

        foreach (var (input, expected) in cases)
        {
            Assert.True(engine.TryQuery(input, 10, out var candidates), input);
            Assert.Contains(expected, candidates.Take(5).Select(candidate => candidate.Text));
        }
    }

    [Fact]
    public void Extended_engine_maps_punctuation_like_the_common_engine()
    {
        var engine = new ExtendedPinyinImeEngine();
        Assert.True(engine.TryMapPunctuation(',', out var mapped));
        Assert.Equal("，", mapped);
        Assert.False(engine.TryMapPunctuation('@', out _));
    }

    [Fact]
    public void The_extended_dictionary_resource_is_embedded()
    {
        var names = typeof(ExtendedPinyinImeEngine).Assembly.GetManifestResourceNames();
        Assert.Contains(
            "TouchKeyboard.Ime.Pinyin.Extended.Resources.pinyin-extended.bin",
            names);
    }

    [Fact]
    public void The_extended_notice_file_exists_and_records_the_sources()
    {
        var path = Path.Combine(
            GoldenSetTests.RepositoryRoot(),
            "src",
            "TouchKeyboard.Ime.Pinyin.Extended",
            "NOTICE");
        Assert.True(File.Exists(path), path);

        var notice = File.ReadAllText(path);
        Assert.Contains("Unicode License", notice);
        Assert.Contains("jieba", notice);
        Assert.Contains("MIT", notice);
    }

    [Fact]
    public void A_session_can_use_the_extended_engine_with_one_line()
    {
        var session = new KeyboardSession(new KeyboardSessionOptions
        {
            Profile = InputProfiles.Text,
            ImeEngine = new ExtendedPinyinImeEngine()
        });

        foreach (var letter in "houtougu")
        {
            session.Execute(KeyCommand.Insert(letter.ToString()));
        }

        Assert.True(session.State.Ime!.IsComposing);
        session.SelectImeCandidate(0);
        Assert.Equal("猴头菇", session.State.Text);
    }
}
