using Application.DTOs.Lead;
using Application.Validators.Lead;
using FluentAssertions;

namespace UnitTests.Validators;

// FR-L1: the property's state is a two-letter code.
public class CreateLeadRequestValidatorTests
{
    private static readonly CreateLeadRequestValidator Validator = new();

    private static CreateLeadRequest ValidRequest() => new()
    {
        FirstName = "Jane",
        LastName = "Doe",
        Email = "jane.doe@example.com",
        PhoneNumber = "+1 512 555 0100",
        PropertyStreet = "100 Congress Ave",
        PropertyCity = "Austin",
        PropertyState = "TX",
        PropertyZipCode = "78701",
        PropertyCountry = "US",
        PropertyLatitude = 30.264m,
        PropertyLongitude = -97.743m,
        PropertyType = "House",
        IsPropertyOwner = true,
        MonthlyBillRange = "$100-$200",
        BestTimeToContact = "Morning"
    };

    [Theory]
    [InlineData("TX")]
    [InlineData("DC")]
    [InlineData("WY")]
    public void Validate_StateIsAStateCode_IsValid(string state)
    {
        // Arrange
        var request = ValidRequest();
        request.PropertyState = state;

        // Act
        var result = Validator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("Texas")]
    [InlineData("tx")]
    [InlineData("PR")] // a territory, not a state
    [InlineData("T")]
    public void Validate_StateIsNotAStateCode_FailsOnTheState(string state)
    {
        // Arrange
        var request = ValidRequest();
        request.PropertyState = state;

        // Act
        var result = Validator.Validate(request);

        // Assert
        result.Errors.Should().ContainSingle().Which.PropertyName.Should().Be(nameof(CreateLeadRequest.PropertyState));
    }

    [Fact]
    public void Validate_StateIsEmpty_FailsOnlyAsRequired()
    {
        // Arrange
        var request = ValidRequest();
        request.PropertyState = "";

        // Act
        var result = Validator.Validate(request);

        // Assert
        result.Errors.Should().ContainSingle().Which.ErrorMessage.Should().Be("State is required");
    }
}
