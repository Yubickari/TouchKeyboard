using System;
using System.IO;
using System.Linq;
using TouchKeyboard.Core;
using TouchKeyboard.Ime.Pinyin;
using Xunit;

namespace TouchKeyboard.Ime.Pinyin.Tests;

public sealed class PinyinImeEngineTests
{
    private static readonly PinyinImeEngine Engine = new PinyinImeEngine();

    [Fact]
    public void Language_tag_is_simplified_chinese()
        => Assert.Equal("zh-Hans", Engine.LanguageTag);

    [Theory]
    [InlineData("nihao", "你好")]
    [InlineData("beijing", "北京")]
    [InlineData("yinhang", "银行")]
    [InlineData("changcheng", "长城")]
    [InlineData("yinyue", "音乐")]
    [InlineData("chongqing", "重庆")]
    [InlineData("xiamen", "厦门")]
    [InlineData("liaojie", "了解")]
    [InlineData("shijian", "时间")]
    [InlineData("wendu", "温度")]
    public void Full_pinyin_returns_the_expected_top_candidate(string input, string expected)
    {
        Assert.True(Engine.TryQuery(input, 10, out var candidates));
        Assert.Equal(expected, candidates[0].Text);
    }

    [Fact]
    public void Initial_abbreviation_ranks_beijing_first()
    {
        Assert.True(Engine.TryQuery("bj", 10, out var candidates));
        Assert.Equal("北京", candidates[0].Text);
    }

    [Fact]
    public void Sentence_segmentation_combines_words()
    {
        Assert.True(Engine.TryQuery("woaibeijing", 10, out var candidates));
        Assert.Equal("我爱北京", candidates[0].Text);
    }

    [Fact]
    public void Uppercase_input_is_accepted()
    {
        Assert.True(Engine.TryQuery("NIHao", 10, out var candidates));
        Assert.Equal("你好", candidates[0].Text);
    }

    [Theory]
    [InlineData("")]
    [InlineData("123")]
    [InlineData("nihao!")]
    [InlineData("zzzz")]
    public void Empty_malformed_and_unknown_input_returns_no_candidates(string input)
    {
        Assert.False(Engine.TryQuery(input, 10, out var candidates));
        Assert.Empty(candidates);
    }

    [Fact]
    public void Null_input_returns_no_candidates()
    {
        Assert.False(Engine.TryQuery(null!, 10, out var candidates));
        Assert.Empty(candidates);
    }

    [Fact]
    public void Zero_max_candidates_returns_no_candidates()
    {
        Assert.False(Engine.TryQuery("nihao", 0, out var candidates));
        Assert.Empty(candidates);
    }

    [Fact]
    public void Query_respects_the_candidate_limit()
    {
        Assert.True(Engine.TryQuery("shi", 3, out var candidates));
        Assert.Equal(3, candidates.Count);
        Assert.All(candidates, candidate => Assert.False(string.IsNullOrWhiteSpace(candidate.Text)));
    }

    [Fact]
    public void Repeated_queries_are_deterministic()
    {
        Assert.True(Engine.TryQuery("woaibeijing", 10, out var first));
        Assert.True(Engine.TryQuery("woaibeijing", 10, out var second));

        Assert.Equal(
            first.Select(candidate => candidate.Text),
            second.Select(candidate => candidate.Text));
    }

    [Fact]
    public void Common_punctuation_maps_to_full_width_and_technical_symbols_stay_ascii()
    {
        var mapped = new (char Input, string Expected)[]
        {
            (',', "，"),
            ('.', "。"),
            ('?', "？"),
            ('!', "！"),
            (':', "："),
            (';', "；"),
            ('(', "（"),
            (')', "）"),
            ('[', "【"),
            (']', "】"),
            ('{', "｛"),
            ('}', "｝"),
            ('<', "《"),
            ('>', "》")
        };

        foreach (var (input, expected) in mapped)
        {
            Assert.True(Engine.TryMapPunctuation(input, out var output));
            Assert.Equal(expected, output);
        }

        foreach (var input in "@#$%^&*-_=+/\\|~\"'".ToCharArray())
        {
            Assert.False(Engine.TryMapPunctuation(input, out _));
        }
    }

    [Fact]
    public void The_common_dictionary_resource_is_embedded()
    {
        var names = typeof(PinyinImeEngine).Assembly.GetManifestResourceNames();
        Assert.Contains(
            "TouchKeyboard.Ime.Pinyin.Resources.pinyin-common.bin",
            names);
    }

    [Fact]
    public void The_notice_file_exists_and_records_the_sources()
    {
        var path = Path.Combine(
            GoldenSetTests.RepositoryRoot(),
            "src",
            "TouchKeyboard.Ime.Pinyin",
            "NOTICE");
        Assert.True(File.Exists(path), path);

        var notice = File.ReadAllText(path);
        Assert.Contains("Unicode License", notice);
        Assert.Contains("jieba", notice);
        Assert.Contains("MIT", notice);
    }

    [Fact]
    public void A_session_can_type_pinyin_and_commit_a_candidate()
    {
        var session = new KeyboardSession(new KeyboardSessionOptions
        {
            Profile = InputProfiles.Text,
            ImeEngine = Engine
        });

        foreach (var letter in "nihao")
        {
            session.Execute(KeyCommand.Insert(letter.ToString()));
        }

        Assert.Equal("nihao", session.State.Ime!.Input);
        Assert.Equal("你好", session.State.Ime.Candidates[0].Text);

        session.Execute(KeyCommand.Insert(" "));
        Assert.Equal("你好", session.State.Text);
        Assert.False(session.State.Ime!.IsComposing);
    }

    [Fact]
    public void A_session_can_commit_a_sentence_candidate()
    {
        var session = new KeyboardSession(new KeyboardSessionOptions
        {
            Profile = InputProfiles.Text,
            ImeEngine = Engine
        });

        foreach (var letter in "woaibeijing")
        {
            session.Execute(KeyCommand.Insert(letter.ToString()));
        }

        Assert.Equal("我爱北京", session.State.Ime!.Candidates[0].Text);
        session.SelectImeCandidate(0);
        Assert.Equal("我爱北京", session.State.Text);
    }
}
