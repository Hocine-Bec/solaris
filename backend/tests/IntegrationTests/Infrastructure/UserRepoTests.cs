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

    [Theory]
    [InlineData(true, true)]
    [InlineData(false, false)]
    public async Task IsActiveAsync_ExistingAccount_ReturnsWhetherItIsActive(bool isActive, bool expected)
    {
        // Arrange
        var user = NewUser(isActive: isActive);
        await SeedAsync(user);

        // Act
        var active = await Repo.IsActiveAsync(user.Id);

        // Assert
        active.Should().Be(expected);
    }

    [Fact]
    public async Task IsActiveAsync_UnknownId_ReturnsFalse()
    {
        // Arrange
        await SeedAsync(NewUser());

        // Act
        var active = await Repo.IsActiveAsync(404);

        // Assert
        active.Should().BeFalse();
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
