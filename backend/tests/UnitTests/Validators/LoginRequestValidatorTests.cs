using Application.DTOs.Auth;
using Application.Validators.Auth;
using FluentAssertions;

namespace UnitTests.Validators;

// Signing in checks only that both were given and fit: the password rule applies when a password is set.
public class LoginRequestValidatorTests
{
    private readonly LoginRequestValidator _validator = new();

    [Fact]
    public void Validate_PasswordBreakingTheRule_PassesBecauseSignInDoesNotCheckTheRule()
    {
        // Act
        var result = _validator.Validate(new LoginRequest { Email = "rosa@example.com", Password = "short" });

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_EmptyFields_NamesBoth()
    {
        // Act
        var result = _validator.Validate(new LoginRequest());

        // Assert
        result.Errors.Select(e => e.ErrorMessage).Should().BeEquivalentTo("Email is required", "Password is required");
    }

    [Fact]
    public void Validate_PasswordLongerThan128_IsRefusedBeforeHashing()
    {
        // Act
        var result = _validator.Validate(new LoginRequest { Email = "rosa@example.com", Password = new string('a', 129) });

        // Assert
        result.Errors.Should().ContainSingle().Which.ErrorMessage.Should().Be("Password must be at most 128 characters");
    }
}
