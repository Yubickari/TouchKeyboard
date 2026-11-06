using System;
using System.Collections.Generic;
using System.Globalization;

namespace TouchKeyboard.Core;

/// <summary>
/// Culture-neutral codes describing why an input profile rejected a value or a
/// commit. The frontend localizes the message; the core never returns UI copy.
/// </summary>
public enum InputValidationErrorCode
{
    /// <summary>An empty value is not accepted.</summary>
    EmptyNotAllowed,

    /// <summary>The text is not a valid integer.</summary>
    InvalidInteger,

    /// <summary>The text is not a valid decimal number.</summary>
    InvalidDecimal,

    /// <summary>The number has more fractional digits than the profile allows.</summary>
    TooManyFractionDigits,

    /// <summary>The text is not a valid date.</summary>
    InvalidDate,

    /// <summary>The text is not a valid IPv4 address.</summary>
    InvalidIpAddress,

    /// <summary>The text is not a valid ASCII token.</summary>
    InvalidAsciiToken,

    /// <summary>The password contains characters outside printable ASCII.</summary>
    InvalidPassword,

    /// <summary>The parsed value is below the control's minimum.</summary>
    ValueTooSmall,

    /// <summary>The parsed value is above the control's maximum.</summary>
    ValueTooLarge,

    /// <summary>The text exceeds the effective maximum length.</summary>
    MaxLengthExceeded,

    /// <summary>The host rejected the value while writing it back.</summary>
    CommitRejected
}

/// <summary>
/// A validation failure returned by an <see cref="InputProfile"/> or by a host
/// adapter. The code and arguments are stable; <see cref="FallbackMessage"/> is
/// English-only and exists for non-localized consumers.
/// </summary>
public sealed class InputValidationError
{
    /// <summary>Creates an error with an optional argument list.</summary>
    public InputValidationError(InputValidationErrorCode code, params object?[] arguments)
    {
        Code = code;
        Arguments = Array.AsReadOnly(arguments ?? Array.Empty<object?>());
        FallbackMessage = CreateFallbackMessage(code, arguments);
    }

    /// <summary>The machine-readable error code.</summary>
    public InputValidationErrorCode Code { get; }

    /// <summary>Arguments referenced by the code, such as the minimum or maximum.</summary>
    public IReadOnlyList<object?> Arguments { get; }

    /// <summary>An English fallback message for non-UI consumers.</summary>
    public string FallbackMessage { get; }

    /// <inheritdoc />
    public override string ToString() => FallbackMessage;

    private static string CreateFallbackMessage(InputValidationErrorCode code, object?[]? arguments)
    {
        var first = arguments is { Length: > 0 } ? arguments[0] : null;
        var text = first is null ? string.Empty : Convert.ToString(first, CultureInfo.InvariantCulture) ?? string.Empty;

        switch (code)
        {
            case InputValidationErrorCode.EmptyNotAllowed:
                return "A value is required.";
            case InputValidationErrorCode.InvalidInteger:
                return "Enter a valid integer.";
            case InputValidationErrorCode.InvalidDecimal:
                return "Enter a valid number.";
            case InputValidationErrorCode.TooManyFractionDigits:
                return $"At most {text} decimal places are allowed.";
            case InputValidationErrorCode.InvalidDate:
                return "Enter a valid date.";
            case InputValidationErrorCode.InvalidIpAddress:
                return "Enter a valid IPv4 address.";
            case InputValidationErrorCode.InvalidAsciiToken:
                return "Only letters, digits and / _ . : - are allowed.";
            case InputValidationErrorCode.InvalidPassword:
                return "The password contains characters outside printable ASCII.";
            case InputValidationErrorCode.ValueTooSmall:
                return $"The value must be at least {text}.";
            case InputValidationErrorCode.ValueTooLarge:
                return $"The value must be at most {text}.";
            case InputValidationErrorCode.MaxLengthExceeded:
                return $"At most {text} characters are allowed.";
            case InputValidationErrorCode.CommitRejected:
                return "The value could not be committed.";
            default:
                return "The value is not valid.";
        }
    }
}
