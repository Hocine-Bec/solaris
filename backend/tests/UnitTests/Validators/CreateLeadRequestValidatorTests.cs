using Application.DTOs.Lead;
using Application.Validators.Lead;
using FluentAssertions;

namespace UnitTests.Validators;

// FR-L1: the property's state is a two-letter code. The coordinates come only with a picked address suggestion. The
// fields the welcome email repeats can't hold a web or email address.
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

    [Fact]
    public void Validate_NoCoordinates_IsValid()
    {
        // Arrange - a typed address, no suggestion picked
        var request = ValidRequest();
        request.PropertyLatitude = null;
        request.PropertyLongitude = null;

        // Act
        var result = Validator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(30.264, null, nameof(CreateLeadRequest.PropertyLongitude))]
    [InlineData(null, -97.743, nameof(CreateLeadRequest.PropertyLatitude))]
    public void Validate_OnlyOneCoordinate_FailsOnTheMissingOne(double? latitude, double? longitude, string missing)
    {
        // Arrange
        var request = ValidRequest();
        request.PropertyLatitude = (decimal?)latitude;
        request.PropertyLongitude = (decimal?)longitude;

        // Act
        var result = Validator.Validate(request);

        // Assert
        result.Errors.Should().ContainSingle().Which.PropertyName.Should().Be(missing);
    }

    [Theory]
    [InlineData(90.5, -97.743, nameof(CreateLeadRequest.PropertyLatitude))]
    [InlineData(30.264, -180.5, nameof(CreateLeadRequest.PropertyLongitude))]
    public void Validate_CoordinateOutOfRange_FailsOnIt(double latitude, double longitude, string wrong)
    {
        // Arrange
        var request = ValidRequest();
        request.PropertyLatitude = (decimal)latitude;
        request.PropertyLongitude = (decimal)longitude;

        // Act
        var result = Validator.Validate(request);

        // Assert
        result.Errors.Should().ContainSingle().Which.PropertyName.Should().Be(wrong);
    }

    [Theory]
    [InlineData("FirstName", "Visit evil.example", "First name")]
    [InlineData("FirstName", "Jane https://x", "First name")]
    [InlineData("LastName", "Doe at verify@evil.example", "Last name")]
    [InlineData("PropertyStreet", "Log in at evil.co/verify", "Street")]
    [InlineData("PropertyCity", "evil\uFF0Ecom", "City")]
    [InlineData("MonthlyBillRange", "see evil.io", "Monthly bill range")]
    public void Validate_RepeatedFieldHoldingAWebAddress_IsRefused(string field, string value, string label)
    {
        // Arrange - the welcome email repeats these fields to an address nobody has confirmed
        var request = ValidRequest();
        typeof(CreateLeadRequest).GetProperty(field)!.SetValue(request, System.Text.RegularExpressions.Regex.Unescape(value));

        // Act
        var result = Validator.Validate(request);

        // Assert
        result.Errors.Should().ContainSingle().Which.ErrorMessage.Should().Be($"{label} can't contain a web or email address");
    }

    [Theory]
    [InlineData("FirstName", "J. R.")]
    [InlineData("LastName", "O'Brien-Smith")]
    [InlineData("PropertyStreet", "1600 N.W. 3rd Ave, Apt 4.5")]
    [InlineData("PropertyStreet", "12 St. John St #3")]
    [InlineData("PropertyCity", "St. Louis")]
    [InlineData("MonthlyBillRange", "$100-$200")]
    public void Validate_RepeatedFieldWithDotsThatAreNotAnAddress_IsValid(string field, string value)
    {
        // A dot followed by a space, a single letter or a number isn't the end of a domain
        var request = ValidRequest();
        typeof(CreateLeadRequest).GetProperty(field)!.SetValue(request, value);

        Validator.Validate(request).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_NoteHoldingAWebAddress_IsValid()
    {
        // Only the office's alert shows the note
        var request = ValidRequest();
        request.Notes = "Photos of the roof: https://photos.example/roof";

        Validator.Validate(request).IsValid.Should().BeTrue();
    }
}
