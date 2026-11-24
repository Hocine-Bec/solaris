using System.Security.Cryptography;
using System.Text;
using Application.Common.Security;
using FluentAssertions;

namespace UnitTests.Security;

// A device key's format, and what is kept of it (BR-T2, ADR 14).
public class DeviceKeysTests
{
    [Fact]
    public void Create_AKeyOfAPrefixAndASecret_AndOnlyTheSecretsHashToStore()
    {
        var key = DeviceKeys.Create();

        key.Text.Should().MatchRegex("^[a-z2-7]{12}\\.[A-Za-z0-9_-]{43}$");
        key.Prefix.Should().Be(key.Text[..12]);
        key.Hash.Should().Equal(SHA256.HashData(Encoding.ASCII.GetBytes(key.Text[13..])));
        DeviceKeys.TryParse(key.Text, out var prefix, out var secret).Should().BeTrue();
        (prefix, DeviceKeys.Matches(secret, key.Hash)).Should().Be((key.Prefix, true));
    }

    [Fact]
    public void Create_EveryKeyIsNew()
    {
        var keys = Enumerable.Range(0, 1000).Select(_ => DeviceKeys.Create()).ToList();

        keys.Select(k => k.Prefix).Should().OnlyHaveUniqueItems();
        keys.Select(k => k.Text[13..]).Should().OnlyHaveUniqueItems();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("ost46n6gr6ny")]
    [InlineData("ost46n6gr6ny.mexkZJqRqA8vDdRO3rZsIDFDl0BhmvStDmX66DDALG")]      // a character short
    [InlineData("ost46n6gr6ny.mexkZJqRqA8vDdRO3rZsIDFDl0BhmvStDmX66DDALG4=")]    // padded
    [InlineData("OST46N6GR6NY.mexkZJqRqA8vDdRO3rZsIDFDl0BhmvStDmX66DDALG4")]     // the prefix upper-cased
    [InlineData("ost46n6gr6n1.mexkZJqRqA8vDdRO3rZsIDFDl0BhmvStDmX66DDALG4")]     // 1 isn't base32
    [InlineData("ost46n6gr6ny-mexkZJqRqA8vDdRO3rZsIDFDl0BhmvStDmX66DDALG4")]     // no dot
    [InlineData("ost46n6gr6ny.mexkZJqRqA8vDdRO3rZsIDFDl0BhmvStDmX66DDAL+4")]     // base64, not base64url
    [InlineData(" ost46n6gr6ny.mexkZJqRqA8vDdRO3rZsIDFDl0BhmvStDmX66DDALG4")]
    public void TryParse_AnythingNotWrittenAsAKey_IsNoKey(string? text)
    {
        DeviceKeys.TryParse(text, out _, out _).Should().BeFalse();
    }

    [Fact]
    public void Matches_AnotherSecret_IsNotTheKey()
    {
        var key = DeviceKeys.Create();
        var other = DeviceKeys.Create();

        DeviceKeys.Matches(other.Text[13..], key.Hash).Should().BeFalse();
    }
}
