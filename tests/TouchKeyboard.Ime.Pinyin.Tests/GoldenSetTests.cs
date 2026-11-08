using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TouchKeyboard.Core;
using TouchKeyboard.Ime.Pinyin;
using Xunit;

namespace TouchKeyboard.Ime.Pinyin.Tests;

public sealed class GoldenSetTests
{
    // 100–200 common words and phrases; top-1 ≥ 80% and top-3 ≥ 95% are
    // development targets, not a quality guarantee.
    private static readonly (string Pinyin, string Expected)[] GoldenSet =
    {
        ("nihao", "你好"),
        ("xiexie", "谢谢"),
        ("zhongguo", "中国"),
        ("beijing", "北京"),
        ("shanghai", "上海"),
        ("guangzhou", "广州"),
        ("shenzhen", "深圳"),
        ("tianjin", "天津"),
        ("chongqing", "重庆"),
        ("shijian", "时间"),
        ("wendu", "温度"),
        ("yinhang", "银行"),
        ("changcheng", "长城"),
        ("yinyue", "音乐"),
        ("xiamen", "厦门"),
        ("liaojie", "了解"),
        ("difang", "地方"),
        ("dizhi", "地址"),
        ("diqiu", "地球"),
        ("daxue", "大学"),
        ("zhongyao", "重要"),
        ("chongxin", "重新"),
        ("hangye", "行业"),
        ("kuaile", "快乐"),
        ("juede", "觉得"),
        ("dedao", "得到"),
        ("mudi", "目的"),
        ("chuli", "处理"),
        ("daochu", "到处"),
        ("weile", "为了"),
        ("jieshu", "结束"),
        ("diaocha", "调查"),
        ("tiaozheng", "调整"),
        ("cengjing", "曾经"),
        ("haishi", "还是"),
        ("haiyou", "还有"),
        ("pianyi", "便宜"),
        ("fangbian", "方便"),
        ("yinggai", "应该"),
        ("gaoxing", "高兴"),
        ("xingfen", "兴奋"),
        ("shuzi", "数字"),
        ("zhongjian", "中间"),
        ("zhangda", "长大"),
        ("xiaozhang", "校长"),
        ("chengzhang", "成长"),
        ("changjiang", "长江"),
        ("daifu", "大夫"),
        ("dishi", "的士"),
        ("dique", "的确"),
        ("shuijiao", "睡觉"),
        ("chengdu", "成都"),
        ("changsha", "长沙"),
        ("changchun", "长春"),
        ("xian", "西安"),
        ("hangzhou", "杭州"),
        ("nanjing", "南京"),
        ("wuhan", "武汉"),
        ("suzhou", "苏州"),
        ("qingdao", "青岛"),
        ("dalian", "大连"),
        ("shenyang", "沈阳"),
        ("haerbin", "哈尔滨"),
        ("wulumuqi", "乌鲁木齐"),
        ("lasa", "拉萨"),
        ("kunming", "昆明"),
        ("guiyang", "贵阳"),
        ("nanning", "南宁"),
        ("fuzhou", "福州"),
        ("jinan", "济南"),
        ("hefei", "合肥"),
        ("nanchang", "南昌"),
        ("zhengzhou", "郑州"),
        ("zhuhai", "珠海"),
        ("sanya", "三亚"),
        ("haikou", "海口"),
        ("lanzhou", "兰州"),
        ("xining", "西宁"),
        ("yinchuan", "银川"),
        ("diannao", "电脑"),
        ("shouji", "手机"),
        ("dianhua", "电话"),
        ("dianshi", "电视"),
        ("bingxiang", "冰箱"),
        ("kongtiao", "空调"),
        ("huoche", "火车"),
        ("feiji", "飞机"),
        ("qiche", "汽车"),
        ("zixingche", "自行车"),
        ("yiyuan", "医院"),
        ("xuexiao", "学校"),
        ("laoshi", "老师"),
        ("xuesheng", "学生"),
        ("pengyou", "朋友"),
        ("jiating", "家庭"),
        ("gongzuo", "工作"),
        ("shenghuo", "生活"),
        ("xuexi", "学习"),
        ("tianqi", "天气"),
        ("xiayu", "下雨"),
        ("taiyang", "太阳"),
        ("yueliang", "月亮"),
        ("xingxing", "星星"),
        ("pingguo", "苹果"),
        ("xiangjiao", "香蕉"),
        ("mifan", "米饭"),
        ("miantiao", "面条"),
        ("kafei", "咖啡"),
        ("niunai", "牛奶"),
        ("jidan", "鸡蛋"),
        ("shucai", "蔬菜"),
        ("shuiguo", "水果"),
        ("yifu", "衣服"),
        ("xiezi", "鞋子"),
        ("wangluo", "网络"),
        ("ruanjian", "软件"),
        ("yingjian", "硬件"),
        ("xitong", "系统"),
        ("shuju", "数据"),
        ("wenjian", "文件"),
        ("tupian", "图片"),
        ("dianying", "电影"),
        ("youxi", "游戏"),
        ("yundong", "运动"),
        ("youyong", "游泳"),
        ("lvyou", "旅游"),
        ("jiudian", "酒店"),
        ("jichang", "机场"),
        ("chezhan", "车站"),
    };

    [Fact]
    public void Golden_set_meets_the_top1_and_top3_targets()
    {
        var engine = new PinyinImeEngine();
        var top1 = 0;
        var top3 = 0;
        var misses = new List<string>();

        foreach (var (pinyin, expected) in GoldenSet)
        {
            Assert.True(
                engine.TryQuery(pinyin, 10, out var candidates),
                $"No candidates for {pinyin} ({expected}).");
            var texts = candidates.Select(candidate => candidate.Text).ToList();
            if (texts.Count > 0 && texts[0] == expected)
            {
                top1++;
            }

            if (texts.Take(3).Contains(expected))
            {
                top3++;
            }
            else
            {
                misses.Add($"{pinyin} -> {expected} (got {string.Join(" | ", texts.Take(5))})");
            }
        }

        var total = GoldenSet.Length;
        Assert.True(total is >= 100 and <= 200, $"Golden set size is {total}.");
        Assert.True(top1 >= total * 0.80, $"top-1 {top1}/{total}; misses: {string.Join("; ", misses)}");
        Assert.True(top3 >= total * 0.95, $"top-3 {top3}/{total}");
    }

    [Fact]
    public void Common_abbreviations_rank_the_expected_word_in_the_top_three()
    {
        var engine = new PinyinImeEngine();
        var abbreviations = new (string Input, string Expected)[]
        {
            ("bj", "北京"),
            ("sh", "上海"),
            ("gz", "广州"),
            ("sz", "深圳"),
            ("tj", "天津"),
            ("cq", "重庆"),
            ("xm", "厦门"),
            ("cd", "成都"),
            ("hz", "杭州"),
            ("nj", "南京")
        };

        foreach (var (input, expected) in abbreviations)
        {
            Assert.True(engine.TryQuery(input, 10, out var candidates), input);
            Assert.Contains(expected, candidates.Take(3).Select(candidate => candidate.Text));
        }
    }

    internal static string RepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "TouchKeyboard.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the repository root.");
    }
}
