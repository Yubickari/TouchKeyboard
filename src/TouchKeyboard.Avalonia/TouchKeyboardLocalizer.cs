using System;
using System.Collections.Generic;
using System.Globalization;
using TouchKeyboard.Core;

namespace TouchKeyboard.Avalonia;

/// <summary>
/// Resolves named key labels and formats core validation errors for the UI.
/// </summary>
public interface ITouchKeyboardLocalizer
{
    /// <summary>Resolves a resource key such as <c>key.confirm</c>.</summary>
    string Get(string resourceKey);

    /// <summary>Formats a core validation error using the current language.</summary>
    string Format(InputValidationError error);
}

/// <summary>Built-in zh-CN and en-US localizer.</summary>
public sealed class TouchKeyboardLocalizer : ITouchKeyboardLocalizer
{
    private readonly IReadOnlyDictionary<string, string> _strings;
    private readonly bool _chinese;

    private TouchKeyboardLocalizer(bool chinese, IReadOnlyDictionary<string, string> strings)
    {
        _chinese = chinese;
        _strings = strings;
    }

    /// <summary>Simplified Chinese strings.</summary>
    public static TouchKeyboardLocalizer ZhCn { get; } = new TouchKeyboardLocalizer(true, new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["key.backspace"] = "退格",
        ["key.clear"] = "清空",
        ["key.space"] = "空格",
        ["key.confirm"] = "确定",
        ["key.shift"] = "Shift",
        ["key.enter"] = "回车",
        ["key.numbers"] = "123",
        ["key.letters"] = "ABC",
        ["key.symbols"] = "符号",
        ["key.caret.left"] = "左移光标",
        ["key.caret.right"] = "右移光标",
        ["key.step.up"] = "数字加一",
        ["key.step.down"] = "数字减一",
        ["key.sign"] = "±",
        ["key.ime.active"] = "中",
        ["key.ime.inactive"] = "英",
        ["key.ime.active.name"] = "中文输入，切换到英文",
        ["key.ime.inactive.name"] = "英文输入，切换到中文",
        ["ime.candidates"] = "候选词",
        ["ime.previous"] = "上一页候选",
        ["ime.next"] = "下一页候选",
        ["ime.no-candidate"] = "无候选，按空格原样输入",
        ["ime.hint.chinese"] = "拼音输入",
        ["ime.hint.english"] = "英文输入",
        ["ime.candidate.name"] = "第 {0} 个候选，{1}",
        ["ime.page"] = "{0}/{1}",
        ["ime.composing"] = "拼音",
        ["title.edit"] = "编辑",
        ["editor.placeholder"] = "请输入",
        ["editor.show"] = "显示密码",
        ["editor.hide"] = "隐藏密码",
        ["close"] = "×",
        ["close.name"] = "关闭"
    });

    /// <summary>English strings.</summary>
    public static TouchKeyboardLocalizer EnUs { get; } = new TouchKeyboardLocalizer(false, new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["key.backspace"] = "Backspace",
        ["key.clear"] = "Clear",
        ["key.space"] = "Space",
        ["key.confirm"] = "Confirm",
        ["key.shift"] = "Shift",
        ["key.enter"] = "Enter",
        ["key.numbers"] = "123",
        ["key.letters"] = "ABC",
        ["key.symbols"] = "Symbols",
        ["key.caret.left"] = "Move caret left",
        ["key.caret.right"] = "Move caret right",
        ["key.step.up"] = "Increase digit",
        ["key.step.down"] = "Decrease digit",
        ["key.sign"] = "±",
        ["key.ime.active"] = "CN",
        ["key.ime.inactive"] = "EN",
        ["key.ime.active.name"] = "Chinese input, switch to English",
        ["key.ime.inactive.name"] = "English input, switch to Chinese",
        ["ime.candidates"] = "Candidates",
        ["ime.previous"] = "Previous candidates",
        ["ime.next"] = "Next candidates",
        ["ime.no-candidate"] = "No candidates; press space to insert as typed",
        ["ime.hint.chinese"] = "Pinyin input",
        ["ime.hint.english"] = "English input",
        ["ime.candidate.name"] = "Candidate {0}, {1}",
        ["ime.page"] = "{0}/{1}",
        ["ime.composing"] = "Pinyin",
        ["title.edit"] = "Edit",
        ["editor.placeholder"] = "Enter a value",
        ["editor.show"] = "Show password",
        ["editor.hide"] = "Hide password",
        ["close"] = "×",
        ["close.name"] = "Close"
    });

    /// <summary>Picks zh-CN or en-US from the current UI culture.</summary>
    public static TouchKeyboardLocalizer Default
        => CultureInfo.CurrentUICulture.TwoLetterISOLanguageName.Equals("zh", StringComparison.OrdinalIgnoreCase)
            ? ZhCn
            : EnUs;

    /// <summary>Creates a localizer for a culture name such as <c>zh-CN</c>.</summary>
    public static TouchKeyboardLocalizer Create(string? culture)
    {
        if (!string.IsNullOrWhiteSpace(culture)
            && culture.StartsWith("zh", StringComparison.OrdinalIgnoreCase))
        {
            return ZhCn;
        }

        return EnUs;
    }

    /// <inheritdoc />
    public string Get(string resourceKey)
        => _strings.TryGetValue(resourceKey, out var value) ? value : resourceKey;

    /// <inheritdoc />
    public string Format(InputValidationError error)
    {
        if (error is null)
        {
            return string.Empty;
        }

        var argument = error.Arguments.Count > 0 ? error.Arguments[0] : null;
        var text = argument is null ? string.Empty : Convert.ToString(argument, CultureInfo.CurrentCulture) ?? string.Empty;

        if (_chinese)
        {
            switch (error.Code)
            {
                case InputValidationErrorCode.EmptyNotAllowed: return "请输入内容";
                case InputValidationErrorCode.InvalidInteger: return "请输入整数";
                case InputValidationErrorCode.InvalidDecimal: return "请输入有效数字";
                case InputValidationErrorCode.TooManyFractionDigits: return $"最多 {text} 位小数";
                case InputValidationErrorCode.InvalidDate: return "请输入有效日期";
                case InputValidationErrorCode.InvalidIpAddress: return "请输入完整 IPv4 地址";
                case InputValidationErrorCode.InvalidAsciiToken: return "只能输入字母、数字和 / _ . : -";
                case InputValidationErrorCode.InvalidPassword: return "密码包含不支持的字符";
                case InputValidationErrorCode.ValueTooSmall: return $"不能小于 {text}";
                case InputValidationErrorCode.ValueTooLarge: return $"不能大于 {text}";
                case InputValidationErrorCode.MaxLengthExceeded: return $"最多 {text} 个字符";
                case InputValidationErrorCode.CommitRejected: return "无法写回该值";
                default: return "输入无效";
            }
        }

        switch (error.Code)
        {
            case InputValidationErrorCode.EmptyNotAllowed: return "A value is required.";
            case InputValidationErrorCode.InvalidInteger: return "Enter a valid integer.";
            case InputValidationErrorCode.InvalidDecimal: return "Enter a valid number.";
            case InputValidationErrorCode.TooManyFractionDigits: return $"At most {text} decimal places.";
            case InputValidationErrorCode.InvalidDate: return "Enter a valid date.";
            case InputValidationErrorCode.InvalidIpAddress: return "Enter a valid IPv4 address.";
            case InputValidationErrorCode.InvalidAsciiToken: return "Only letters, digits and / _ . : - are allowed.";
            case InputValidationErrorCode.InvalidPassword: return "The password contains unsupported characters.";
            case InputValidationErrorCode.ValueTooSmall: return $"The value must be at least {text}.";
            case InputValidationErrorCode.ValueTooLarge: return $"The value must be at most {text}.";
            case InputValidationErrorCode.MaxLengthExceeded: return $"At most {text} characters.";
            case InputValidationErrorCode.CommitRejected: return "The value could not be written back.";
            default: return "The value is not valid.";
        }
    }
}
