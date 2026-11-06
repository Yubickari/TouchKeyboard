using TouchKeyboard.Core;
using Xunit;

namespace TouchKeyboard.Core.Tests;

public sealed class InputProfileTests
{
    [Fact]
    public void Text_accepts_printable_text_and_rejects_control_characters()
    {
        Assert.True(InputProfiles.Text.IsIntermediateValid("Hello 123"));
        Assert.False(InputProfiles.Text.IsIntermediateValid("a\nb"));
        Assert.True(InputProfiles.Text.TryValidate("Hello", out var normalized, out var error));
        Assert.Equal("Hello", normalized);
        Assert.Null(error);
    }

    [Fact]
    public void Password_accepts_printable_ascii_only()
    {
        Assert.True(InputProfiles.Password.IsIntermediateValid("abc123!@#"));
        Assert.False(InputProfiles.Password.IsIntermediateValid("abc\n"));
        Assert.True(InputProfiles.Password.TryValidate("abc123!@#", out _, out var error));
        Assert.Null(error);
        Assert.False(InputProfiles.Password.TryValidate("密码", out _, out error));
        Assert.Equal(InputValidationErrorCode.InvalidPassword, error!.Code);
    }

    [Fact]
    public void Ascii_token_trims_and_requires_non_empty()
    {
        Assert.True(InputProfiles.AsciiToken.IsIntermediateValid("abc-_/.:"));
        Assert.False(InputProfiles.AsciiToken.IsIntermediateValid("abc!"));
        Assert.True(InputProfiles.AsciiToken.TryValidate("  abc-1  ", out var normalized, out var error));
        Assert.Equal("abc-1", normalized);
        Assert.Null(error);
        Assert.False(InputProfiles.AsciiToken.TryValidate("   ", out _, out error));
        Assert.Equal(InputValidationErrorCode.InvalidAsciiToken, error!.Code);
    }

    [Theory]
    [InlineData("", true)]
    [InlineData("-", true)]
    [InlineData("0", true)]
    [InlineData("-123", true)]
    [InlineData("+1", false)]
    [InlineData("1.2", false)]
    [InlineData("12a", false)]
    public void Integer_intermediate_states(string text, bool expected)
        => Assert.Equal(expected, InputProfiles.Integer.IsIntermediateValid(text));

    [Fact]
    public void Positive_integer_rejects_a_leading_minus()
    {
        Assert.False(InputProfiles.PositiveInteger.IsIntermediateValid("-1"));
        Assert.False(InputProfiles.PositiveInteger.TryValidate("-1", out _, out var error));
        Assert.Equal(InputValidationErrorCode.InvalidInteger, error!.Code);
    }

    [Fact]
    public void Integer_commit_keeps_leading_zeros_and_rejects_empty()
    {
        Assert.True(InputProfiles.Integer.TryValidate(" 007 ", out var normalized, out var error));
        Assert.Equal("007", normalized);
        Assert.Null(error);
        Assert.False(InputProfiles.Integer.TryValidate(string.Empty, out _, out error));
        Assert.Equal(InputValidationErrorCode.EmptyNotAllowed, error!.Code);
    }

    [Theory]
    [InlineData("", true)]
    [InlineData("-", true)]
    [InlineData(".", true)]
    [InlineData("-.", true)]
    [InlineData(".5", true)]
    [InlineData("-0.5", true)]
    [InlineData("1.", true)]
    [InlineData("1.2.3", false)]
    [InlineData("a", false)]
    public void Decimal_intermediate_states(string text, bool expected)
        => Assert.Equal(expected, InputProfiles.Decimal.IsIntermediateValid(text));

    [Fact]
    public void Decimal_commit_normalizes_short_forms()
    {
        Assert.True(InputProfiles.Decimal.TryValidate(".5", out var normalized, out var error));
        Assert.Equal("0.5", normalized);
        Assert.Null(error);

        Assert.True(InputProfiles.Decimal.TryValidate("-.5", out normalized, out error));
        Assert.Equal("-0.5", normalized);
        Assert.Null(error);

        Assert.True(InputProfiles.Decimal.TryValidate("1.", out normalized, out error));
        Assert.Equal("1", normalized);
        Assert.Null(error);
    }

    [Fact]
    public void Decimal_commit_rejects_a_lone_sign()
    {
        Assert.False(InputProfiles.Decimal.TryValidate("-", out _, out var error));
        Assert.Equal(InputValidationErrorCode.InvalidDecimal, error!.Code);
    }

    [Fact]
    public void Decimal_fraction_limit_is_enforced()
    {
        var profile = new DecimalInputProfile(allowNegative: true, maxFractionDigits: 2);

        Assert.True(profile.TryValidate("1.25", out _, out var error));
        Assert.Null(error);

        Assert.False(profile.TryValidate("1.234", out _, out error));
        Assert.Equal(InputValidationErrorCode.TooManyFractionDigits, error!.Code);
    }

    [Theory]
    [InlineData("", true)]
    [InlineData("2026", true)]
    [InlineData("2026/", true)]
    [InlineData("2026/11", true)]
    [InlineData("2026/11/06", true)]
    [InlineData("2026-11-06", false)]
    [InlineData("2026/111/06", false)]
    public void Date_slash_intermediate_states(string text, bool expected)
        => Assert.Equal(expected, InputProfiles.DateSlash.IsIntermediateValid(text));

    [Fact]
    public void Date_slash_commit_normalizes_and_validates()
    {
        Assert.True(InputProfiles.DateSlash.TryValidate("2026/2/3", out var normalized, out var error));
        Assert.Equal("2026/02/03", normalized);
        Assert.Null(error);

        Assert.True(InputProfiles.DateSlash.TryValidate(string.Empty, out normalized, out error));
        Assert.Equal(string.Empty, normalized);
        Assert.Null(error);

        Assert.False(InputProfiles.DateSlash.TryValidate("2026/2/30", out _, out error));
        Assert.Equal(InputValidationErrorCode.InvalidDate, error!.Code);
    }

    [Fact]
    public void Date_dot_uses_its_own_separator()
    {
        Assert.True(InputProfiles.DateDot.IsIntermediateValid("2026.11.06"));
        Assert.False(InputProfiles.DateDot.IsIntermediateValid("2026/11/06"));
        Assert.True(InputProfiles.DateDot.TryValidate("2026.11.06", out var normalized, out var error));
        Assert.Equal("2026/11/06", normalized);
        Assert.Null(error);
    }

    [Theory]
    [InlineData("", true)]
    [InlineData("192", true)]
    [InlineData("192.168.", true)]
    [InlineData("192.168.1.1", true)]
    [InlineData("192.168.1.1.1", false)]
    [InlineData("192.168.1.256", true)]
    [InlineData("a.b.c.d", false)]
    public void Ip_intermediate_states(string text, bool expected)
        => Assert.Equal(expected, InputProfiles.IpAddress.IsIntermediateValid(text));

    [Fact]
    public void Ip_commit_normalizes_and_validates()
    {
        Assert.True(InputProfiles.IpAddress.TryValidate("192.168.001.010", out var normalized, out var error));
        Assert.Equal("192.168.1.10", normalized);
        Assert.Null(error);

        Assert.False(InputProfiles.IpAddress.TryValidate("192.168.1.256", out _, out error));
        Assert.Equal(InputValidationErrorCode.InvalidIpAddress, error!.Code);

        Assert.False(InputProfiles.IpAddress.TryValidate("192.168.1", out _, out error));
        Assert.Equal(InputValidationErrorCode.InvalidIpAddress, error!.Code);

        Assert.Equal(15, InputProfiles.IpAddress.MaxLength);
    }

    [Fact]
    public void Registry_resolves_built_ins_case_insensitively()
    {
        Assert.True(InputProfileRegistry.TryGet("TEXT", out var profile));
        Assert.Same(InputProfiles.Text, profile);

        Assert.True(InputProfileRegistry.TryGet("positive-decimal", out profile));
        Assert.Same(InputProfiles.PositiveDecimal, profile);

        Assert.False(InputProfileRegistry.TryGet("missing", out _));
        Assert.Throws<KeyNotFoundException>(() => InputProfileRegistry.Get("missing"));
        Assert.Throws<InvalidOperationException>(() => InputProfileRegistry.Register(InputProfiles.Text));
    }

    [Fact]
    public void All_built_in_profiles_have_unique_ids()
    {
        Assert.Equal(10, InputProfiles.All.Count);
        Assert.Equal(InputProfiles.All.Count, InputProfiles.All.Select(p => p.Id).Distinct(StringComparer.OrdinalIgnoreCase).Count());
    }
}
