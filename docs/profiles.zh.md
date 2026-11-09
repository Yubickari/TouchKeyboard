# 输入档位

[English](profiles.md) · **简体中文**

`InputProfile` 是面向单个字段的具名契约：字段以哪种键盘模式打开、是否允许使用全键盘、数字小键盘提供哪些按键、哪些编辑状态合法，以及提交值如何校验和规范化。档位不可变，可安全共享；由 `KeyboardSession` 负责应用。

## 内置档位

| 档位 id | 默认模式 | 接受内容 | 提交规范化 | 数字小键盘 | `AllowsIme` |
|---|---|---|---|---|---|
| `text` | 全键盘 | 除控制字符外的任意文本 | 保持不变 | 通用（`+`/`−` 字符、`ABC`、小数点） | **true** |
| `password` | 全键盘 | 可打印 ASCII `0x20`–`0x7E` | 保持不变 | 通用 | false |
| `ascii-token` | 全键盘 | `[A-Za-z0-9/_.:-]*`，非空 | `Trim()` | 通用 | false |
| `integer` | 数字小键盘 | 有符号整数 | 保持输入原样（前导零保留） | 步进键、±、无小数点，`0` 占两列 | false |
| `positive-integer` | 数字小键盘 | 仅数字 | 保持输入原样 | 步进键、无正负号、`0` 占两列 | false |
| `decimal` | 数字小键盘 | 小数点固定为 `.` 的有符号小数 | 按固定区域性解析并重新格式化（`.5` → `0.5`，`1.50` → `1.5`） | 步进键、±、`.` 分隔符 | false |
| `positive-decimal` | 数字小键盘 | 非负小数 | 同 `decimal` | 步进键、无正负号、`.` 分隔符 | false |
| `date-slash` | 数字小键盘 | 编辑时接受 `yyyy/M/d` 分段，提交时须为有效日期 | `yyyy/MM/dd` | 步进键、`/` 分隔符 | false |
| `date-dot` | 数字小键盘 | 同上，分隔符为 `.` | `yyyy/MM/dd`（为适配器规范化为斜杠） | 步进键、`.` 分隔符 | false |
| `ip-address` | 数字小键盘 | 四段 1–3 位数字，取值 `0–255`，最长 15 个字符 | 各段去掉前导零后用 `.` 连接 | 步进键、`.` 分隔符 | false |

`InputProfileRegistry` 解析 id 时不区分大小写。`InputProfiles.All` 按稳定顺序枚举内置档位。

### 职责划分

- **档位**决定有效性、默认模式、数字键集和规范化。
- **会话**将档位与目标字段的元数据结合：生效的最大长度（`min(档位上限, 目标上限)`）、`Minimum`/`Maximum` 范围校验，以及目标字段是否允许负号。
- **适配器**读取目标字段的当前文本和元数据，并把提交后的文本写回。

因此，当目标字段的 `Minimum = 0` 时，`integer` 档位只有在目标字段报告可接受负值的情况下，才会沿用档位键集中的 ± 键；否则会话会忽略正负号切换，适配器也会拒绝超出范围的提交值。

## 编辑态校验与提交校验

每个档位实现两项相互独立的检查：

- `IsIntermediateValid(text)`：输入过程中的这个状态合法吗？例如 `-`、`1.`、`2026/` 和 `192.168.` 都是可编辑状态，尽管它们还不能提交。违反这条规则的插入会被丢弃。
- `TryValidate(text, out normalized, out error)`：这是合法的提交值吗？它还会返回适配器写回时使用的规范化文本。

`KeyboardSession` 在每次插入时调用编辑态检查，只在 `TryCommit` 中调用提交检查。

## 校验错误

核心校验返回与区域性无关的 `InputValidationError`（错误码加参数）。前端负责本地化；`FallbackMessage` 只有英文。

| 错误码 | 含义 |
|---|---|
| `EmptyNotAllowed` | 档位或目标字段要求提供值。 |
| `InvalidInteger` | 文本不是有符号整数。 |
| `InvalidDecimal` | 文本不是有效小数（或不允许正负号）。 |
| `TooManyFractionDigits` | 小数位数超过档位允许的上限（参数：最大位数）。 |
| `InvalidDate` | 分段缺失，或日期不存在。 |
| `InvalidIpAddress` | 不是由四段 `0–255` 组成的地址。 |
| `InvalidAsciiToken` | 包含 `A-Z a-z 0-9 / _ . : -` 之外的字符，或文本为空。 |
| `InvalidPassword` | 包含可打印 ASCII 之外的字符。 |
| `ValueTooSmall` | 解析后的值低于会话 `Minimum`（参数：最小值）。 |
| `ValueTooLarge` | 解析后的值高于会话 `Maximum`（参数：最大值）。 |
| `MaxLengthExceeded` | 超过生效的最大长度（参数：长度上限）。 |
| `CommitRejected` | 适配器或回读拒绝了该值（取消后也会返回该错误）。 |

内置的 `zh-CN` 和 `en-US` 本地化器会把每个错误码映射为一段提示；参见 [avalonia.zh.md](avalonia.zh.md#本地化)。`TouchKeyboardOptions.KeyLanguage` 无需区域性字符串，即可固定使用内置语言。

## 数字小键盘按键集

`Numpad` 键面由 `NumericKeypadOptions` 经 `NumpadSlots` 推导得到：

| 场景 | L0/L1 | L2（左列第 2 行） | R2（右列第 2 行） | 底部槽位 | `0` | 确定 |
|---|---|---|---|---|---|---|
| 通用（从全键盘进入） | `+` `−` 字符 | `ABC` | 空 | `.` | 1 列 | 2 行 |
| 正整数 | ▲ ▼ 步进 | 空 | 空 | 空 | 2 列 | 2 行 |
| 有符号整数 | ▲ ▼ 步进 | `±` | 空 | 空 | 2 列 | 2 行 |
| 非负小数 | ▲ ▼ 步进 | 空 | 空 | `.` | 1 列 | 2 行 |
| 有符号小数 | ▲ ▼ 步进 | `±` | 空 | `.` | 1 列 | 2 行 |
| 日期 | ▲ ▼ 步进 | 空 | 空 | `/` 或 `.` | 1 列 | 2 行 |
| IPv4 | ▲ ▼ 步进 | 空 | 空 | `.` | 1 列 | 2 行 |

该映射由 `NumpadSlots.From(profile.NumericKeypad)` 生成：`ShowSign` 为 `true` 时用 ± 取代 `ABC`，`ShowDecimalPoint` 优先于显式分隔符，`ShowStepKeys` 让左上角的一对键在步进箭头和 `+`/`−` 字符之间切换。几何与跨格规则见 [layouts.zh.md](layouts.zh.md#numpad)。

## 档位契约

| 成员 | 类型 | 用途 |
|---|---|---|
| `Id` | abstract | `TouchKeyboardInput.Profile` 使用的稳定 id。 |
| `DefaultMode` | virtual | 默认返回 `FullKeyboard`；数字档位返回 `NumericKeypad`。 |
| `AllowsFullKeyboard` | virtual | 数字档位返回 `false`（隐藏模式切换和 `ABC`）。 |
| `NumericKeypad` | virtual | 用于 Numpad 槽位映射的 `NumericKeypadOptions`。默认为 `NumericKeypadOptions.General`。 |
| `MaxLength` | virtual | 可选的档位级上限，与目标字段上限共同生效。 |
| `AllowEmpty` | virtual | 是否允许提交空值。 |
| `AllowsIme` | virtual | 是否允许使用可选的托管 IME；默认 `false`。 |
| `IsIntermediateValid` | virtual | 合法的编辑状态。 |
| `TryValidate` | abstract | 提交校验与规范化。 |

`AllowsIme`（virtual，默认 `false`）控制是否启用可选的托管拼音 IME。只有 `text` 返回 `true`；密码、ASCII 标识、整数、小数、日期和 IPv4 档位均保持无 IME。自定义档位可以用 `public override bool AllowsIme => true;` 主动开启，但必须同时允许全键盘：纯数字档位没有可供组合输入使用的字母键面。宿主还必须通过 `options.UseIme(...)` 配置引擎；仅设置 `AllowsIme` 不会创建 IME 键或候选栏。参见 [ime.md](ime.md) 和 [ADR 0004](adr/0004-managed-pinyin-ime.md)。

## 自定义档位

```csharp
public sealed class SerialNumberProfile : InputProfile
{
    public override string Id => "serial-number";
    public override bool AllowEmpty => false;
    public override KeyboardMode DefaultMode => KeyboardMode.FullKeyboard;

    public override bool IsIntermediateValid(string text)
        => text.Length <= 24 && text.All(c => char.IsLetterOrDigit(c) || c is '-' or '_');

    public override bool TryValidate(string text, out string normalized, out InputValidationError? error)
    {
        normalized = text.Trim();
        if (normalized.Length == 0)
        {
            error = new InputValidationError(InputValidationErrorCode.EmptyNotAllowed);
            return false;
        }

        error = null;
        return true;
    }
}

InputProfileRegistry.Register(new SerialNumberProfile());
```

自定义数字字段时，优先复用现有的 `NumericInputProfile` 基类，或自行构造 `NumericKeypadOptions`，以便 Numpad 槽位映射继续生效：

```csharp
var keypad = new NumericKeypadOptions(
    showSign: false,
    showDecimalPoint: false,
    separator: '-',
    showStepKeys: true);

var slots = NumpadSlots.From(keypad);          // or new NumpadSlots(...) for a custom mapping
var layout = KeyboardLayouts.Numpad("numeric-serial", slots);
```

对于非内置场景（例如使用 `:` 分隔符、并在右列第 2 行放置场景键的时间字段），`KeyboardLayouts.Numpad(id, slots)` 是扩展点。
