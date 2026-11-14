using Application.Common.Models;
using Domain.Entities;
using Domain.Enums;
using FluentAssertions;
using Infrastructure.Repositories;
using TestUtilities.Base;
using TestUtilities.Database;

namespace IntegrationTests.Infrastructure;

public class UserRepoTests(PostgresFixture database) : IntegrationTestBase(database)
{
    private UserRepo Repo => new(Context);

    internal static User NewUser(string email = "rosa.castillo@example.com", UserRole role = UserRole.Admin,
        bool isActive = true) => new()
    {
        Name = "Rosa Castillo",
        Email = email,
        NormalizedEmail = User.NormalizeEmail(email),
        PasswordHash = "not-a-real-hash",
        Role = role,
        IsActive = isActive,
        CreatedAt = DateTime.UtcNow
    };

    [Theory]
    [InlineData("rosa.castillo@example.com")]
    [InlineData("ROSA.Castillo@Example.com")]
    [InlineData("  rosa.castillo@example.com ")]
    public async Task GetByEmailAsync_SameEmailWhateverItsCaseOrSpaces_FindsTheAccount(string typed)
    {
        // Arrange
        await SeedAsync(NewUser());

        // Act
        var user = await Repo.GetByEmailAsync(typed);

        // Assert
        user.Should().NotBeNull();
        user!.Email.Should().Be("rosa.castillo@example.com");
    }

    [Fact]
    public async Task GetByEmailAsync_UnknownEmail_ReturnsNull()
    {
        // Arrange
        await SeedAsync(NewUser());

        // Act
        var user = await Repo.GetByEmailAsync("someone.else@example.com");

        // Assert
        user.Should().BeNull();
    }

    [Fact]
    public async Task AnyActiveAdminAsync_OnlyATechnicianAndADeactivatedAdmin_ReturnsFalse()
    {
        // Arrange
        await SeedAsync(NewUser("tech@example.com", UserRole.Technician), NewUser("old.admin@example.com", isActive: false));

        // Act
        var any = await Repo.AnyActiveAdminAsync();

        // Assert
        any.Should().BeFalse();
    }

    [Fact]
    public async Task AnyActiveAdminAsync_WithAnActiveAdmin_ReturnsTrue()
    {
        // Arrange
        await SeedAsync(NewUser());

        // Act
        var any = await Repo.AnyActiveAdminAsync();

        // Assert
        any.Should().BeTrue();
    }

    private static readonly DateTime Now = new(2026, 11, 14, 9, 30, 15, DateTimeKind.Utc);

    [Theory]
    [InlineData(true, true)]
    [InlineData(false, false)]
    public async Task AcceptsSessionAsync_PasswordNeverReplaced_ReturnsWhetherItIsActive(bool isActive, bool expected)
    {
        // Arrange
        var user = NewUser(isActive: isActive);
        await SeedAsync(user);

        // Act
        var accepted = await Repo.AcceptsSessionAsync(user.Id, Now);

        // Assert
        accepted.Should().Be(expected);
    }

    [Fact]
    public async Task AcceptsSessionAsync_UnknownId_ReturnsFalse()
    {
        // Arrange
        await SeedAsync(NewUser());

        // Act
        var accepted = await Repo.AcceptsSessionAsync(404, Now);

        // Assert
        accepted.Should().BeFalse();
    }

    [Theory]
    [InlineData(-1, false)]   // started the second before the new password
    [InlineData(0, true)]     // in the same second: the session that changed its own password keeps its new token
    [InlineData(5, true)]
    public async Task AcceptsSessionAsync_PasswordReplaced_AcceptsOnlySessionsFromThatSecondOn(int startedSecondsAfter,
        bool expected)
    {
        // Arrange - the password replaced at 09:30:15.700: sessions are valid from 09:30:15
        var user = NewUser();
        user.SetPassword("another-hash", Now.AddMilliseconds(700));
        await SeedAsync(user);

        // Act
        var accepted = await Repo.AcceptsSessionAsync(user.Id, Now.AddSeconds(startedSecondsAfter));

        // Assert
        accepted.Should().Be(expected);
    }

    [Fact]
    public async Task Save_TwoAccountsDifferingOnlyByCase_IsADuplicate()
    {
        // Arrange
        await SeedAsync(NewUser("rosa.castillo@example.com"));
        Repo.Add(NewUser("Rosa.Castillo@EXAMPLE.com"));

        // Act
        var outcome = await SaveAsync();

        // Assert - the unique index on the normalized email holds one account per email
        outcome.Should().Be(SaveOutcome.Duplicate);
    }
}
