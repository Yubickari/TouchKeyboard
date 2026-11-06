using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

namespace TouchKeyboard.Core;

/// <summary>
/// The built-in input profiles. Each instance is immutable and safe to share.
/// </summary>
public static class InputProfiles
{
    /// <summary>Free text; printable characters, no IME in v1.</summary>
    public static InputProfile Text { get; } = new TextInputProfile();

    /// <summary>Password text; printable ASCII, masked by the frontend.</summary>
    public static InputProfile Password { get; } = new PasswordInputProfile();

    /// <summary>ASCII identifier: letters, digits and <c>/ _ . : -</c>.</summary>
    public static InputProfile AsciiToken { get; } = new AsciiTokenInputProfile();

    /// <summary>Signed integer.</summary>
    public static InputProfile Integer { get; } = new IntegerInputProfile(allowNegative: true);

    /// <summary>Non-negative integer.</summary>
    public static InputProfile PositiveInteger { get; } = new IntegerInputProfile(allowNegative: false);

    /// <summary>Signed decimal with an invariant decimal point.</summary>
    public static InputProfile Decimal { get; } = new DecimalInputProfile(allowNegative: true);

    /// <summary>Non-negative decimal.</summary>
    public static InputProfile PositiveDecimal { get; } = new DecimalInputProfile(allowNegative: false);

    /// <summary>Date using <c>/</c>.</summary>
    public static InputProfile DateSlash { get; } = new DateInputProfile('/');

    /// <summary>Date using <c>.</c>.</summary>
    public static InputProfile DateDot { get; } = new DateInputProfile('.');

    /// <summary>IPv4 address.</summary>
    public static InputProfile IpAddress { get; } = new IpAddressInputProfile();

    /// <summary>All built-in profiles, in a stable order.</summary>
    public static IReadOnlyList<InputProfile> All { get; } = new List<InputProfile>
    {
        Text,
        Password,
        AsciiToken,
        Integer,
        PositiveInteger,
        Decimal,
        PositiveDecimal,
        DateSlash,
        DateDot,
        IpAddress
    }.AsReadOnly();
}

internal sealed class TextInputProfile : InputProfile
{
    public override string Id => "text";

    public override bool IsIntermediateValid(string text)
    {
        if (text is null)
        {
            return false;
        }

        for (var i = 0; i < text.Length; i++)
        {
            if (char.IsControl(text[i]))
            {
                return false;
            }
        }

        return true;
    }

    public override bool TryValidate(
        string text,
        out string normalized,
        out InputValidationError? error)
    {
        normalized = text ?? string.Empty;
        error = null;
        return true;
    }
}

internal sealed class PasswordInputProfile : InputProfile
{
    public override string Id => "password";

    public override bool IsIntermediateValid(string text) => IsPrintableAscii(text);

    public override bool TryValidate(
        string text,
        out string normalized,
        out InputValidationError? error)
    {
        normalized = text ?? string.Empty;
        error = null;
        if (!IsPrintableAscii(normalized))
        {
            error = new InputValidationError(InputValidationErrorCode.InvalidPassword);
            return false;
        }

        return true;
    }

    private static bool IsPrintableAscii(string? text)
    {
        if (text is null)
        {
            return false;
        }

        for (var i = 0; i < text.Length; i++)
        {
            var c = text[i];
            if (c < 0x20 || c > 0x7E)
            {
                return false;
            }
        }

        return true;
    }
}

internal sealed class AsciiTokenInputProfile : InputProfile
{
    private static readonly Regex Token = new Regex(
        "^[A-Za-z0-9/_.:-]*$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public override string Id => "ascii-token";

    public override bool AllowEmpty => false;

    public override bool IsIntermediateValid(string text) => text is not null && Token.IsMatch(text);

    public override bool TryValidate(
        string text,
        out string normalized,
        out InputValidationError? error)
    {
        normalized = (text ?? string.Empty).Trim();
        error = null;
        if (normalized.Length == 0 || !Token.IsMatch(normalized))
        {
            error = new InputValidationError(InputValidationErrorCode.InvalidAsciiToken);
            return false;
        }

        return true;
    }
}

/// <summary>
/// Base for integer and decimal profiles. Range checks (Minimum/Maximum) are
/// applied by the session because they come from the target control.
/// </summary>
public abstract class NumericInputProfile : InputProfile
{
    /// <summary>Creates a numeric profile.</summary>
    protected NumericInputProfile(
        bool allowsNegative,
        bool allowsDecimalPoint,
        int? maxFractionDigits,
        NumericKeypadOptions keypad)
    {
        if (maxFractionDigits is < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxFractionDigits));
        }

        AllowsNegative = allowsNegative;
        AllowsDecimalPoint = allowsDecimalPoint;
        MaxFractionDigits = maxFractionDigits;
        NumericKeypad = keypad ?? throw new ArgumentNullException(nameof(keypad));
    }

    /// <summary>Whether a leading minus sign is accepted.</summary>
    public bool AllowsNegative { get; }

    /// <summary>Whether a single decimal point is accepted.</summary>
    public bool AllowsDecimalPoint { get; }

    /// <summary>Optional fractional digit limit.</summary>
    public int? MaxFractionDigits { get; }

    /// <inheritdoc />
    public override KeyboardMode DefaultMode => KeyboardMode.NumericKeypad;

    /// <inheritdoc />
    public override bool AllowsFullKeyboard => false;

    /// <inheritdoc />
    public override NumericKeypadOptions NumericKeypad { get; }

    /// <inheritdoc />
    public override bool AllowEmpty => false;

    /// <inheritdoc />
    public override bool IsIntermediateValid(string text)
    {
        if (text is null)
        {
            return false;
        }

        var index = 0;
        if (text.Length > 0 && text[0] == '-')
        {
            if (!AllowsNegative)
            {
                return false;
            }

            index++;
        }

        var seenDecimalPoint = false;
        for (; index < text.Length; index++)
        {
            var c = text[index];
            if (c >= '0' && c <= '9')
            {
                continue;
            }

            if (c == '.' && AllowsDecimalPoint && !seenDecimalPoint)
            {
                seenDecimalPoint = true;
                continue;
            }

            return false;
        }

        return true;
    }

    /// <inheritdoc />
    public override bool TryValidate(
        string text,
        out string normalized,
        out InputValidationError? error)
    {
        normalized = (text ?? string.Empty).Trim();
        error = null;

        if (normalized.Length == 0)
        {
            error = new InputValidationError(InputValidationErrorCode.EmptyNotAllowed);
            return false;
        }

        if (!AllowsNegative && normalized.StartsWith("-", StringComparison.Ordinal))
        {
            error = new InputValidationError(
                AllowsDecimalPoint
                    ? InputValidationErrorCode.InvalidDecimal
                    : InputValidationErrorCode.InvalidInteger);
            return false;
        }

        if (AllowsDecimalPoint)
        {
            var parseText = normalized;
            if (parseText.StartsWith(".", StringComparison.Ordinal))
            {
                parseText = "0" + parseText;
            }
            else if (parseText.StartsWith("-.", StringComparison.Ordinal))
            {
                parseText = "-0" + parseText.Substring(1);
            }

            if (!decimal.TryParse(
                    parseText,
                    NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint,
                    CultureInfo.InvariantCulture,
                    out var value))
            {
                error = new InputValidationError(InputValidationErrorCode.InvalidDecimal);
                return false;
            }

            if (MaxFractionDigits is { } max && CountFractionDigits(normalized) > max)
            {
                error = new InputValidationError(InputValidationErrorCode.TooManyFractionDigits, max);
                return false;
            }

            normalized = value.ToString(CultureInfo.InvariantCulture);
            return true;
        }

        if (!long.TryParse(
                normalized,
                NumberStyles.AllowLeadingSign,
                CultureInfo.InvariantCulture,
                out _))
        {
            error = new InputValidationError(InputValidationErrorCode.InvalidInteger);
            return false;
        }

        // Keep the user's digits; do not strip leading zeros.
        return true;
    }

    private static int CountFractionDigits(string text)
    {
        var point = text.IndexOf('.');
        return point < 0 ? 0 : text.Length - point - 1;
    }
}

internal sealed class IntegerInputProfile : NumericInputProfile
{
    public IntegerInputProfile(bool allowNegative)
        : base(allowNegative, allowsDecimalPoint: false, maxFractionDigits: null,
            NumericKeypadOptions.Integer(allowNegative))
    {
    }

    public override string Id => AllowsNegative ? "integer" : "positive-integer";
}

internal sealed class DecimalInputProfile : NumericInputProfile
{
    public DecimalInputProfile(bool allowNegative, int? maxFractionDigits = null)
        : base(allowNegative, allowsDecimalPoint: true, maxFractionDigits,
            NumericKeypadOptions.Decimal(allowNegative))
    {
    }

    public override string Id => AllowsNegative ? "decimal" : "positive-decimal";
}

internal sealed class DateInputProfile : InputProfile
{
    private readonly Regex _intermediate;

    public DateInputProfile(char separator)
    {
        if (separator != '/' && separator != '.' && separator != '-')
        {
            throw new ArgumentOutOfRangeException(nameof(separator), "Date separator must be /, . or -.");
        }

        Separator = separator;
        _intermediate = new Regex(
            "^\\d{0,4}(" + Regex.Escape(separator.ToString()) + "\\d{0,2}){0,2}$",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);
        NumericKeypad = NumericKeypadOptions.Date(separator);
    }

    public char Separator { get; }

    public override string Id => Separator switch
    {
        '/' => "date-slash",
        '.' => "date-dot",
        _ => "date-dash"
    };

    public override KeyboardMode DefaultMode => KeyboardMode.NumericKeypad;

    public override bool AllowsFullKeyboard => false;

    public override NumericKeypadOptions NumericKeypad { get; }

    public override bool IsIntermediateValid(string text)
        => text is not null && _intermediate.IsMatch(text);

    public override bool TryValidate(
        string text,
        out string normalized,
        out InputValidationError? error)
    {
        normalized = (text ?? string.Empty).Trim();
        error = null;

        if (normalized.Length == 0)
        {
            return true;
        }

        var parts = normalized.Split(new[] { Separator }, StringSplitOptions.None);
        if (parts.Length != 3
            || parts[0].Length != 4
            || parts[1].Length < 1 || parts[1].Length > 2
            || parts[2].Length < 1 || parts[2].Length > 2)
        {
            error = new InputValidationError(InputValidationErrorCode.InvalidDate);
            return false;
        }

        if (!int.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out var year)
            || !int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out var month)
            || !int.TryParse(parts[2], NumberStyles.None, CultureInfo.InvariantCulture, out var day)
            || year < 1 || year > 9999
            || month < 1 || month > 12
            || day < 1 || day > DateTime.DaysInMonth(year, month))
        {
            error = new InputValidationError(InputValidationErrorCode.InvalidDate);
            return false;
        }

        normalized = new DateTime(year, month, day).ToString("yyyy/MM/dd", CultureInfo.InvariantCulture);
        return true;
    }
}

internal sealed class IpAddressInputProfile : InputProfile
{
    public override string Id => "ip-address";

    public override KeyboardMode DefaultMode => KeyboardMode.NumericKeypad;

    public override bool AllowsFullKeyboard => false;

    public override NumericKeypadOptions NumericKeypad { get; } = NumericKeypadOptions.IpAddress();

    public override int? MaxLength => 15;

    public override bool AllowEmpty => false;

    public override bool IsIntermediateValid(string text)
    {
        if (text is null)
        {
            return false;
        }

        var dots = 0;
        var digitsInPart = 0;
        for (var i = 0; i < text.Length; i++)
        {
            var c = text[i];
            if (c == '.')
            {
                dots++;
                if (dots > 3)
                {
                    return false;
                }

                digitsInPart = 0;
                continue;
            }

            if (c < '0' || c > '9')
            {
                return false;
            }

            digitsInPart++;
            if (digitsInPart > 3)
            {
                return false;
            }
        }

        return true;
    }

    public override bool TryValidate(
        string text,
        out string normalized,
        out InputValidationError? error)
    {
        normalized = (text ?? string.Empty).Trim();
        error = null;

        var parts = normalized.Split(new[] { '.' }, StringSplitOptions.None);
        if (parts.Length != 4)
        {
            error = new InputValidationError(InputValidationErrorCode.InvalidIpAddress);
            return false;
        }

        var normalizedParts = new string[4];
        for (var i = 0; i < parts.Length; i++)
        {
            if (parts[i].Length < 1 || parts[i].Length > 3
                || !byte.TryParse(parts[i], NumberStyles.None, CultureInfo.InvariantCulture, out var value))
            {
                error = new InputValidationError(InputValidationErrorCode.InvalidIpAddress);
                return false;
            }

            normalizedParts[i] = value.ToString(CultureInfo.InvariantCulture);
        }

        normalized = string.Join(".", normalizedParts);
        return true;
    }
}
