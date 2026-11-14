using Domain.Entities;
using FluentAssertions;

namespace UnitTests.Domain;

// A new password replaces the hash and ends the sessions started before its second (ADR 7).
public class UserTests
{
    [Fact]
    public void SetPassword_ReplacesTheHash_AndSessionsAreValidFromTheStartOfThatSecond()
    {
        // Arrange
        var user = new User { PasswordHash = "old-hash" };
        var now = new DateTime(2026, 11, 14, 9, 30, 15, 742, DateTimeKind.Utc).AddTicks(1234);

        // Act
        user.SetPassword("new-hash", now);

        // Assert - a session token's issue time is whole seconds, so this one is compared in whole seconds too
        user.PasswordHash.Should().Be("new-hash");
        user.SessionsValidFrom.Should().Be(new DateTime(2026, 11, 14, 9, 30, 15, DateTimeKind.Utc));
        user.SessionsValidFrom!.Value.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Fact]
    public void NewAccount_SessionsValidFrom_IsNullUntilAPasswordIsReplaced()
    {
        new User().SessionsValidFrom.Should().BeNull();
    }
}
