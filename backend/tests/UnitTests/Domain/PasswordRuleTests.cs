using Domain.Common;
using FluentAssertions;

namespace UnitTests.Domain;

// The password rule: at least 8 characters, an uppercase letter, a number and a special character; 128 at most.
public class PasswordRuleTests
{
    [Theory]
    [InlineData("Solaris#2026")]
    [InlineData("Aa1!aaaa")]                    // exactly 8
    [InlineData("Sun and panels 4 ever!")]      // spaces are allowed, they just don't count as special
    [InlineData("Ünïcödé-Pässwörd-9")]          // letters beyond ASCII count as letters
    public void Problems_PasswordFollowingTheRule_IsEmpty(string password)
    {
        // Act
        var problems = PasswordRule.Problems(password);

        // Assert
        problems.Should().BeEmpty();
        PasswordRule.IsValid(password).Should().BeTrue();
    }

    [Theory]
    [InlineData("Aa1!aaa", "Password must be at least 8 characters")]
    [InlineData("solaris#2026", "Password must contain an uppercase letter")]
    [InlineData("Solaris#year", "Password must contain a number")]
    [InlineData("Solaris 2026", "Password must contain a special character, such as ! or #")]
    public void Problems_PasswordMissingOneRequirement_NamesIt(string password, string problem)
    {
        // Act
        var problems = PasswordRule.Problems(password);

        // Assert
        problems.Should().Equal(problem);
    }

    [Fact]
    public void Problems_PasswordLongerThan128_IsTooLong()
    {
        // Arrange
        var password = "Aa1!" + new string('a', 125);

        // Act
        var problems = PasswordRule.Problems(password);

        // Assert
        problems.Should().Equal("Password must be at most 128 characters");
        PasswordRule.Problems("Aa1!" + new string('a', 124)).Should().BeEmpty();
    }

    [Fact]
    public void Problems_PasswordMissingEverything_ListsEachProblemInOrder()
    {
        // Act
        var problems = PasswordRule.Problems("abc");

        // Assert
        problems.Should().Equal(
            "Password must be at least 8 characters",
            "Password must contain an uppercase letter",
            "Password must contain a number",
            "Password must contain a special character, such as ! or #");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Problems_NoPassword_IsRequired(string? password)
    {
        // Act
        var problems = PasswordRule.Problems(password);

        // Assert
        problems.Should().Equal("Password is required");
    }
}
