using Application.DTOs.Auth;
using Application.DTOs.Users;
using Application.Validators.Auth;
using Application.Validators.Users;
using FluentAssertions;

namespace UnitTests.Validators;

// The fields of an account, whoever sets them: a name, an email, and a password that follows the password rule.
public class UserRequestValidatorTests
{
    private static CreateUserRequest Lena(Action<CreateUserRequest>? change = null)
    {
        var request = new CreateUserRequest { Name = "Lena Park", Email = "lena.park@example.com", Password = "Panels#2026" };
        change?.Invoke(request);
        return request;
    }

    private static IEnumerable<string> MessagesOf(FluentValidation.Results.ValidationResult result, string field) =>
        result.Errors.Where(e => e.PropertyName == field).Select(e => e.ErrorMessage);

    #region A new technician

    [Fact]
    public void Create_ValidRequest_Passes()
    {
        new CreateUserRequestValidator().Validate(Lena()).IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("", "Name is required")]
    [InlineData("   ", "Name is required")]
    public void Create_NoName_SaysItIsRequired(string name, string message)
    {
        var result = new CreateUserRequestValidator().Validate(Lena(r => r.Name = name));

        MessagesOf(result, "Name").Should().Equal(message);
    }

    [Fact]
    public void Create_NameOf101CharactersAfterTrimming_IsTooLong()
    {
        var result = new CreateUserRequestValidator().Validate(Lena(r => r.Name = "  " + new string('a', 101) + "  "));

        MessagesOf(result, "Name").Should().Equal("Name must be at most 100 characters");
    }

    [Fact]
    public void Create_NameOf100CharactersWithSpacesAround_Passes()
    {
        var result = new CreateUserRequestValidator().Validate(Lena(r => r.Name = "  " + new string('a', 100) + "  "));

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("", "Email is required")]
    [InlineData("lena.park", "Email must be an email address, such as name@example.com")]
    [InlineData("lena park@example.com", "Email must be an email address, such as name@example.com")]
    [InlineData("@example.com", "Email must be an email address, such as name@example.com")]
    public void Create_EmailNotAnEmail_SaysWhy(string email, string message)
    {
        var result = new CreateUserRequestValidator().Validate(Lena(r => r.Email = email));

        MessagesOf(result, "Email").Should().Equal(message);
    }

    [Fact]
    public void Create_EmailWithSpacesAround_Passes()
    {
        new CreateUserRequestValidator().Validate(Lena(r => r.Email = "  lena.park@example.com ")).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Create_EmailLongerThan255_IsTooLong()
    {
        var result = new CreateUserRequestValidator().Validate(Lena(r => r.Email = new string('a', 244) + "@example.com"));

        MessagesOf(result, "Email").Should().Equal("Email must be at most 255 characters");
    }

    [Fact]
    public void Create_PasswordBreakingTheRule_NamesEachThingItMisses()
    {
        var result = new CreateUserRequestValidator().Validate(Lena(r => r.Password = "panels"));

        MessagesOf(result, "Password").Should().Equal(
            "Password must be at least 8 characters", "Password must contain an uppercase letter",
            "Password must contain a number", "Password must contain a special character, such as ! or #");
    }

    #endregion

    #region Editing, and a new password from the office

    [Fact]
    public void Update_EmptyFields_NamesBoth()
    {
        var result = new UpdateUserRequestValidator().Validate(new UpdateUserRequest());

        result.Errors.Select(e => e.ErrorMessage).Should().BeEquivalentTo("Name is required", "Email is required");
    }

    [Fact]
    public void SetPassword_Empty_IsRequired()
    {
        var result = new SetPasswordRequestValidator().Validate(new SetPasswordRequest());

        result.Errors.Should().ContainSingle().Which.ErrorMessage.Should().Be("Password is required");
    }

    [Fact]
    public void SetPassword_FollowingTheRule_Passes()
    {
        new SetPasswordRequestValidator().Validate(new SetPasswordRequest { Password = "Inverter#77" }).IsValid.Should().BeTrue();
    }

    #endregion

    #region Changing your own password

    [Fact]
    public void ChangePassword_CurrentOneBreakingTheRule_PassesBecauseOnlyTheNewOneFollowsIt()
    {
        // An account made before the rule, or by a looser one, can still change its password
        var result = new ChangePasswordRequestValidator().Validate(
            new ChangePasswordRequest { CurrentPassword = "old", NewPassword = "Inverter#77" });

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ChangePassword_EmptyFields_NamesBoth()
    {
        var result = new ChangePasswordRequestValidator().Validate(new ChangePasswordRequest());

        result.Errors.Select(e => (e.PropertyName, e.ErrorMessage)).Should().BeEquivalentTo(
            [("CurrentPassword", "Current password is required"), ("NewPassword", "Password is required")]);
    }

    [Fact]
    public void ChangePassword_CurrentOneLongerThan128_IsRefusedBeforeHashing()
    {
        var result = new ChangePasswordRequestValidator().Validate(
            new ChangePasswordRequest { CurrentPassword = new string('a', 129), NewPassword = "Inverter#77" });

        result.Errors.Should().ContainSingle().Which.ErrorMessage
            .Should().Be("Current password must be at most 128 characters");
    }

    #endregion
}
