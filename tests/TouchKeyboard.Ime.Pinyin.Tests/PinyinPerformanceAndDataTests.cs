using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using TouchKeyboard.Ime.Pinyin;
using Xunit;

namespace TouchKeyboard.Ime.Pinyin.Tests;

public sealed class PinyinPerformanceAndDataTests
{
    [Fact]
    public void Corrupt_dictionary_data_fails_fast()
    {
        var assembly = typeof(PinyinPerformanceAndDataTests).Assembly;
        Assert.Throws<InvalidDataException>(
            () => PinyinDictionary.Load(assembly, "TouchKeyboard.Ime.Pinyin.Tests.Resources.broken.bin"));
        Assert.Throws<InvalidDataException>(
            () => PinyinDictionary.Load(assembly, "TouchKeyboard.Ime.Pinyin.Tests.Resources.missing.bin"));
    }

    [Fact]
    public void Common_queries_stay_within_the_development_latency_target()
    {
        var engine = new PinyinImeEngine();

        // Warm up the lazy load, JIT and the indexes before measuring.
        foreach (var input in Inputs)
        {
            engine.TryQuery(input, 10, out _);
        }

        var samples = new double[Inputs.Length * 20];
        var index = 0;
        foreach (var input in Inputs)
        {
            for (var repeat = 0; repeat < 20; repeat++)
            {
                var stopwatch = Stopwatch.StartNew();
                engine.TryQuery(input, 10, out _);
                stopwatch.Stop();
                samples[index++] = stopwatch.Elapsed.TotalMilliseconds;
            }
        }

        Array.Sort(samples);
        var p95 = samples[(int)(samples.Length * 0.95) - 1];
        Assert.True(p95 < 25, $"p95 {p95:F3} ms (development target 5 ms)");
    }

    [Fact]
    public void Extended_queries_stay_within_the_extended_latency_target()
    {
        var engine = new TouchKeyboard.Ime.Pinyin.Extended.ExtendedPinyinImeEngine();
        engine.TryQuery("houtougu", 10, out _);

        var samples = new double[200];
        for (var i = 0; i < samples.Length; i++)
        {
            var stopwatch = Stopwatch.StartNew();
            engine.TryQuery(Inputs[i % Inputs.Length], 10, out _);
            stopwatch.Stop();
            samples[i] = stopwatch.Elapsed.TotalMilliseconds;
        }

        Array.Sort(samples);
        var p95 = samples[(int)(samples.Length * 0.95) - 1];
        Assert.True(p95 < 50, $"p95 {p95:F3} ms (development target 10 ms)");
    }

    [Fact]
    public void Common_dictionary_load_finishes_within_the_development_target()
    {
        var stopwatch = Stopwatch.StartNew();
        var dictionary = PinyinDictionary.Load(
            typeof(PinyinImeEngine).Assembly,
            PinyinImeEngine.CommonDictionaryResource);
        stopwatch.Stop();

        Assert.True(dictionary.Count > 0);
        Assert.True(
            stopwatch.Elapsed.TotalMilliseconds < 3000,
            $"load {stopwatch.Elapsed.TotalMilliseconds:F1} ms (development target 50 ms)");
    }

    [Fact]
    public void Extended_dictionary_load_finishes_within_the_development_target()
    {
        var stopwatch = Stopwatch.StartNew();
        var dictionary = PinyinDictionary.Load(
            typeof(TouchKeyboard.Ime.Pinyin.Extended.ExtendedPinyinImeEngine).Assembly,
            TouchKeyboard.Ime.Pinyin.Extended.ExtendedPinyinImeEngine.ExtendedDictionaryResource);
        stopwatch.Stop();

        Assert.True(dictionary.Count > 0);
        Assert.True(
            stopwatch.Elapsed.TotalMilliseconds < 5000,
            $"load {stopwatch.Elapsed.TotalMilliseconds:F1} ms (development target 200 ms)");
    }

    private static readonly string[] Inputs =
    {
        "nihao", "beijing", "bj", "woaibeijing", "yinhang", "changcheng",
        "yinyue", "chongqing", "xiamen", "shijian", "wendu", "zhongguo",
        "shanghai", "xian", "hang", "shuiguo", "diannao", "jintian",
        "mingtian", "xiexie"
    };
}
