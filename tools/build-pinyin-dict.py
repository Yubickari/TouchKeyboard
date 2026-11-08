#!/usr/bin/env python3
"""Build the embedded pinyin dictionaries for TouchKeyboard.Ime.Pinyin.

Sources
-------
* Unihan_Readings.txt (kMandarin) from the Unicode Character Database.
  License: Unicode License v3 (see the package NOTICE).
* jieba's dict.txt for the word list and frequency initial values.
  License: MIT (see the package NOTICE).

The phrase readings are generated from the character readings plus a small,
project-owned polyphone override table. No third-party phrase-pinyin table is
copied. The generated binaries are deterministic for a given source snapshot.

Usage:
    python3 tools/build-pinyin-dict.py \
        --unihan path/to/Unihan_Readings.txt \
        --jieba path/to/dict.txt \
        --common src/TouchKeyboard.Ime.Pinyin/Resources/pinyin-common.bin \
        --extended src/TouchKeyboard.Ime.Pinyin.Extended/Resources/pinyin-extended.bin
"""

from __future__ import annotations

import argparse
import hashlib
import math
import re
import struct
import sys
import unicodedata
import zlib
from pathlib import Path

CJK_RE = re.compile(r"^[\u3400-\u9fff]+$")
LETTERS_RE = re.compile(r"^[a-z]+$")

# Project-owned corrections for common polyphonic words. Only words whose
# generated reading would be wrong (or which are useful for abbreviation tests)
# need to be listed; the rest use the Unihan primary reading.
POLYPHONE_WORDS = {
    "银行": "yin'hang",
    "行长": "hang'zhang",
    "行业": "hang'ye",
    "行情": "hang'qing",
    "内行": "nei'hang",
    "同行": "tong'hang",
    "长城": "chang'cheng",
    "长江": "chang'jiang",
    "长安": "chang'an",
    "长沙": "chang'sha",
    "长春": "chang'chun",
    "长度": "chang'du",
    "音乐": "yin'yue",
    "乐器": "yue'qi",
    "乐曲": "yue'qu",
    "快乐": "kuai'le",
    "乐园": "le'yuan",
    "乐观": "le'guan",
    "重庆": "chong'qing",
    "重复": "chong'fu",
    "重新": "chong'xin",
    "厦门": "xia'men",
    "大厦": "da'sha",
    "了解": "liao'jie",
    "地方": "di'fang",
    "地址": "di'zhi",
    "地球": "di'qiu",
    "地区": "di'qu",
    "地铁": "di'tie",
    "地图": "di'tu",
    "目的": "mu'di",
    "的确": "di'que",
    "的士": "di'shi",
    "觉得": "jue'de",
    "睡觉": "shui'jiao",
    "得到": "de'dao",
    "得意": "de'yi",
    "大夫": "dai'fu",
    "重要": "zhong'yao",
    "重量": "zhong'liang",
    "处理": "chu'li",
    "到处": "dao'chu",
    "结束": "jie'shu",
    "结实": "jie'shi",
    "调查": "diao'cha",
    "调整": "tiao'zheng",
    "空调": "kong'tiao",
    "还是": "hai'shi",
    "还有": "hai'you",
    "还给": "huan'gei",
    "便宜": "pian'yi",
    "方便": "fang'bian",
    "应该": "ying'gai",
    "高兴": "gao'xing",
    "兴奋": "xing'fen",
    "数字": "shu'zi",
    "数数": "shu'shu",
    "中间": "zhong'jian",
    "成长": "cheng'zhang",
    "校长": "xiao'zhang",
    "长大": "zhang'da",
    "生长": "sheng'zhang",
    "家长": "jia'zhang",
    "长相": "zhang'xiang",
    "曾经": "ceng'jing",
    "参差": "cen'ci",
    "出差": "chu'chai",
    "差别": "cha'bie",
    "说服": "shuo'fu",
    "游说": "you'shui",
    "似的": "shi'de",
    "着急": "zhao'ji",
    "着火": "zhao'huo",
    "穿着": "chuan'zhuo",
    "看着": "kan'zhe",
    "传记": "zhuan'ji",
    "传说": "chuan'shuo",
    "都市": "du'shi",
    "都是": "dou'shi",
    "投降": "tou'xiang",
    "下降": "xia'jiang",
    "卷发": "juan'fa",
    "试卷": "shi'juan",
    "朝阳": "chao'yang",
    "朝气": "zhao'qi",
    "薄弱": "bo'ruo",
    "薄饼": "bao'bing",
    "湖泊": "hu'po",
    "停泊": "ting'bo",
    "萝卜": "luo'bo",
    "刹车": "sha'che",
    "刹那": "cha'na",
    "禅让": "shan'rang",
    "颤栗": "zhan'li",
    "称心": "chen'xin",
    "对称": "dui'cheng",
    "盛开": "sheng'kai",
    "盛饭": "cheng'fan",
    "畜牧": "xu'mu",
    "家畜": "jia'chu",
    "攒钱": "zan'qian",
    "弹琴": "tan'qin",
    "子弹": "zi'dan",
    "厌恶": "yan'wu",
    "恶劣": "e'lie",
    "给予": "ji'yu",
    "供给": "gong'ji",
    "龟裂": "jun'lie",
    "恐吓": "kong'he",
    "剥削": "bo'xue",
    "削皮": "xiao'pi",
    "校对": "jiao'dui",
    "学校": "xue'xiao",
    "呜咽": "wu'ye",
    "钥匙": "yao'shi",
    "锁钥": "suo'yue",
    "择菜": "zhai'cai",
    "选择": "xuan'ze",
    "包扎": "bao'zha",
    "挣扎": "zheng'zha",
    "粘稠": "nian'chou",
    "粘贴": "zhan'tie",
    "仔细": "zi'xi",
    "牛仔": "niu'zai",
    "成都": "cheng'du",
}

# Additional readings for common polyphonic characters. Unihan kMandarin has a
# single primary reading; these alternates let a user type either reading and
# get the character. The values are tone-free lower-case pinyin.
CHAR_READINGS = {
    "长": ["chang", "zhang"],
    "行": ["xing", "hang"],
    "重": ["zhong", "chong"],
    "乐": ["le", "yue"],
    "厦": ["sha", "xia"],
    "地": ["de", "di"],
    "了": ["le", "liao"],
    "着": ["zhe", "zhao", "zhuo"],
    "得": ["de", "dei"],
    "和": ["he", "huo"],
    "数": ["shu", "shuo"],
    "差": ["cha", "chai", "ci"],
    "参": ["can", "shen", "cen"],
    "曾": ["ceng", "zeng"],
    "还": ["hai", "huan"],
    "会": ["hui", "kuai"],
    "解": ["jie", "xie"],
    "落": ["luo", "la", "lao"],
    "没": ["mei", "mo"],
    "模": ["mo", "mu"],
    "哪": ["na", "nei"],
    "那": ["na", "nei"],
    "期": ["qi", "ji"],
    "强": ["qiang", "jiang"],
    "亲": ["qin", "qing"],
    "省": ["sheng", "xing"],
    "石": ["shi", "dan"],
    "属": ["shu", "zhu"],
    "说": ["shuo", "shui"],
    "似": ["si", "shi"],
    "宿": ["su", "xiu"],
    "汤": ["tang", "shang"],
    "提": ["ti", "di"],
    "藏": ["cang", "zang"],
    "处": ["chu", "chu"],
    "传": ["chuan", "zhuan"],
    "调": ["diao", "tiao"],
    "都": ["dou", "du"],
    "度": ["du", "duo"],
    "降": ["jiang", "xiang"],
    "卷": ["juan", "quan"],
    "便": ["bian", "pian"],
    "朝": ["chao", "zhao"],
    "大": ["da", "dai"],
    "薄": ["bao", "bo"],
    "辟": ["pi", "bi"],
    "扁": ["bian", "pian"],
    "泊": ["bo", "po"],
    "卜": ["bu", "bo"],
    "刹": ["cha", "sha"],
    "禅": ["chan", "shan"],
    "颤": ["chan", "zhan"],
    "车": ["che", "ju"],
    "称": ["cheng", "chen"],
    "乘": ["cheng", "sheng"],
    "盛": ["sheng", "cheng"],
    "臭": ["chou", "xiu"],
    "畜": ["chu", "xu"],
    "绰": ["chuo", "chao"],
    "攒": ["cuan", "zan"],
    "撮": ["cuo", "zuo"],
    "弹": ["dan", "tan"],
    "佃": ["dian", "tian"],
    "读": ["du", "dou"],
    "恶": ["e", "wu"],
    "否": ["fou", "pi"],
    "脯": ["fu", "pu"],
    "给": ["gei", "ji"],
    "颈": ["jing", "geng"],
    "龟": ["gui", "jun"],
    "虾": ["xia", "ha"],
    "吓": ["xia", "he"],
    "削": ["xiao", "xue"],
    "校": ["xiao", "jiao"],
    "血": ["xue", "xie"],
    "咽": ["yan", "ye"],
    "钥": ["yao", "yue"],
    "叶": ["ye", "xie"],
    "择": ["ze", "zhai"],
    "扎": ["zha", "za"],
    "粘": ["zhan", "nian"],
    "幢": ["zhuang", "chuang"],
    "仔": ["zi", "zai"],
}

# Words that get the maximum frequency so that a common abbreviation such as
# "bj" prefers 北京 over 比较 and everyday words stay at the top of a page.
CURATED = {
    "我", "你", "他", "她", "它", "是", "在", "有", "不", "的", "了", "和",
    "你好", "谢谢", "中国", "北京", "上海", "广州", "深圳", "天津", "重庆",
    "时间", "温度", "银行", "长城", "音乐", "厦门", "了解", "地方", "地址",
    "大学", "重要", "重新", "行业", "快乐", "觉得", "得到", "目的", "处理",
    "到处", "为了", "结束", "调查", "调整", "曾经", "还是", "还有", "便宜",
    "方便", "应该", "高兴", "兴奋", "数字", "中间", "长大", "校长", "成长",
    "长江", "大夫", "的士", "的确", "睡觉", "成都", "长沙", "长春", "西安",
    "杭州", "南京", "武汉", "苏州", "青岛", "大连", "沈阳", "哈尔滨",
    "乌鲁木齐", "拉萨", "昆明", "贵阳", "南宁", "福州", "济南", "合肥",
    "南昌", "郑州", "珠海", "三亚", "海口", "兰州", "西宁", "银川",
}

def strip_tone(raw: str) -> str | None:
    """Turn a Unihan tone-marked reading into tone-free lower-case pinyin."""
    out: list[str] = []
    for char in raw.strip():
        if char == "ü":
            out.append("v")
            continue

        decomposition = unicodedata.normalize("NFD", char)
        if "\u0308" in decomposition:
            # u + diaeresis (ü) with an optional tone mark -> v for input.
            out.append("v")
            continue

        base = "".join(
            part for part in decomposition if not unicodedata.combining(part)
        ).lower()
        if base == "ü":
            base = "v"
        out.append(base)

    value = "".join(out)
    return value if LETTERS_RE.match(value) else None


def read_unihan(path: Path) -> dict[str, list[str]]:
    readings: dict[str, list[str]] = {}
    with path.open(encoding="utf-8") as handle:
        for line in handle:
            if "kMandarin" not in line:
                continue
            parts = line.rstrip("\n").split("\t")
            if len(parts) < 3 or parts[1] != "kMandarin":
                continue
            code_point = int(parts[0][2:], 16)
            if not 0x3400 <= code_point <= 0x9FFF:
                continue
            char = chr(code_point)
            values: list[str] = []
            for raw in parts[2].split():
                value = strip_tone(raw)
                if value is not None and value not in values:
                    values.append(value)
            if values:
                readings[char] = values
    return readings


def read_jieba(path: Path) -> list[tuple[str, int, str]]:
    rows: list[tuple[str, int, str]] = []
    seen: set[str] = set()
    with path.open(encoding="utf-8") as handle:
        for line in handle:
            parts = line.rstrip("\n").split(" ")
            if len(parts) < 2:
                continue
            word = parts[0]
            if word in seen or not CJK_RE.match(word) or len(word) > 6:
                continue
            try:
                frequency = int(parts[1])
            except ValueError:
                continue
            if frequency <= 0:
                continue
            tag = parts[2] if len(parts) > 2 else ""
            if tag.startswith("w"):
                continue
            seen.add(word)
            rows.append((word, frequency, tag))

    rows.sort(key=lambda row: (-row[1], row[0]))
    return rows


def word_pinyin(word: str, readings: dict[str, list[str]]) -> str | None:
    if word in POLYPHONE_WORDS:
        return POLYPHONE_WORDS[word]
    syllables: list[str] = []
    for char in word:
        values = readings.get(char)
        if not values:
            return None
        syllables.append(values[0])
    return "'".join(syllables)


def collect_entries(
    rows: list[tuple[str, int, str]],
    readings: dict[str, list[str]],
    limit: int,
) -> tuple[dict[tuple[str, str], int], int]:
    selected = rows[:limit]
    selected_words = {word for word, _, _ in selected}

    # Ensure the hand-curated words and the polyphone override words exist even
    # if the upstream frequency list skips one of them.
    manual = set(CURATED) | set(POLYPHONE_WORDS)
    for word in sorted(manual):
        if word not in selected_words and CJK_RE.match(word):
            if all(char in readings or char in CHAR_READINGS for char in word):
                selected.append((word, 0, "manual"))
                selected_words.add(word)

    max_frequency = max((frequency for _, frequency, _ in selected), default=1)
    log_max = math.log1p(max_frequency)
    entries: dict[tuple[str, str], int] = {}

    def scaled(frequency: int) -> int:
        if frequency <= 0:
            return 1200
        if frequency in (0, max_frequency):
            return 65000
        return max(1, min(65000, round(65000 * math.log1p(frequency) / log_max)))

    def add(word: str, pinyin: str, frequency: int) -> None:
        if word in CURATED:
            frequency = 65535
        key = (word, pinyin)
        if frequency > entries.get(key, 0):
            entries[key] = frequency

    for word, frequency, _ in selected:
        value = scaled(frequency)
        if len(word) == 1:
            values = list(CHAR_READINGS.get(word, []))
            for reading in readings.get(word, []):
                if reading not in values:
                    values.append(reading)
            for reading in values:
                add(word, reading, value)
            continue

        pinyin = word_pinyin(word, readings)
        if pinyin is not None:
            add(word, pinyin, value)

    return entries, max_frequency


def write_dictionary(path: Path, entries: dict[tuple[str, str], int]) -> tuple[int, int]:
    path.parent.mkdir(parents=True, exist_ok=True)
    ordered = sorted(entries.items(), key=lambda item: (-item[1], item[0][0], item[0][1]))
    raw = bytearray()
    raw += b"TKPINYIN\x01"
    raw += struct.pack("<I", len(ordered))
    for (text, pinyin), frequency in ordered:
        text_bytes = text.encode("utf-8")
        pinyin_bytes = pinyin.encode("ascii")
        if len(text_bytes) > 255 or len(pinyin_bytes) > 255:
            raise ValueError(f"entry too long: {text} / {pinyin}")
        raw += bytes([len(text_bytes)]) + text_bytes
        raw += bytes([len(pinyin_bytes)]) + pinyin_bytes
        raw += struct.pack("<H", frequency)

    compressor = zlib.compressobj(9, zlib.DEFLATED, -15)
    compressed = compressor.compress(raw) + compressor.flush()
    path.write_bytes(compressed)
    return len(raw), len(compressed)


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--unihan", required=True, type=Path)
    parser.add_argument("--jieba", required=True, type=Path)
    parser.add_argument("--common", required=True, type=Path)
    parser.add_argument("--extended", required=True, type=Path)
    parser.add_argument("--common-limit", type=int, default=20000)
    parser.add_argument("--extended-limit", type=int, default=100000)
    args = parser.parse_args()

    readings = read_unihan(args.unihan)
    rows = read_jieba(args.jieba)
    print(f"unihan characters: {len(readings)}")
    print(f"jieba han words:   {len(rows)}")

    common, _ = collect_entries(rows, readings, args.common_limit)
    extended, _ = collect_entries(rows, readings, args.extended_limit)

    common_raw, common_compressed = write_dictionary(args.common, common)
    extended_raw, extended_compressed = write_dictionary(args.extended, extended)

    print(f"common:   {len(common)} entries, {common_raw} -> {common_compressed} bytes")
    print(f"extended: {len(extended)} entries, {extended_raw} -> {extended_compressed} bytes")

    # A short, reproducible source record for the generated resource. The full
    # licence texts live in the package NOTICE.
    print("sources:")
    for label, path in (("Unihan_Readings.txt", args.unihan), ("jieba dict.txt", args.jieba)):
        digest = hashlib.sha256(path.read_bytes()).hexdigest()
        print(f"  {label}: sha256 {digest}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
