using Application.DTOs.Customers;
using Application.DTOs.Lead;
using Application.Validators.Customers;
using Application.Validators.Lead;
using FluentAssertions;

namespace UnitTests.Validators;

// What the staff app sends about leads and customers: a status, corrected contact details, a list's filter.
public class LeadRequestValidatorTests
{
    private static IEnumerable<string> MessagesOf(FluentValidation.Results.ValidationResult result, string field) =>
        result.Errors.Where(e => e.PropertyName == field).Select(e => e.ErrorMessage);

    private static UpdateLeadRequest Dana(Action<UpdateLeadRequest>? change = null)
    {
        var request = new UpdateLeadRequest
            { FirstName = "Dana", LastName = "Whitfield", Email = "dana.whitfield@example.com", Phone = "(512) 555-0187" };
        change?.Invoke(request);
        return request;
    }

    [Theory]
    [InlineData("Contacted")]
    [InlineData("Disqualified")]
    [InlineData("Lost")]
    public void Status_AMoveByName_Passes(string status)
    {
        new UpdateLeadStatusRequestValidator().Validate(new UpdateLeadStatusRequest { Status = status }).IsValid
            .Should().BeTrue();
    }

    [Theory]
    [InlineData("Converted", "Convert the lead instead: converting also creates its customer and installation")]
    [InlineData("New", "Status must be one of: Contacted, Disqualified, Lost")]
    [InlineData("contacted", "Status must be one of: Contacted, Disqualified, Lost")]
    [InlineData("", "Status is required")]
    public void Status_AnythingElse_SaysWhy(string status, string message)
    {
        var result = new UpdateLeadStatusRequestValidator().Validate(new UpdateLeadStatusRequest { Status = status });

        MessagesOf(result, "Status").Should().Equal(message);
    }

    [Fact]
    public void Edit_ValidRequest_Passes()
    {
        new UpdateLeadRequestValidator().Validate(Dana()).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Edit_TheFormsRules_ApplyToEachField()
    {
        var result = new UpdateLeadRequestValidator().Validate(Dana(r =>
        {
            r.FirstName = "D";
            r.LastName = "visit evil.example";
            r.Email = "dana";
            r.Phone = "call me";
        }));

        MessagesOf(result, "FirstName").Should().Equal("First name must be at least 2 characters");
        MessagesOf(result, "LastName").Should().ContainSingle();
        MessagesOf(result, "Email").Should().Equal("Invalid email format");
        MessagesOf(result, "Phone").Should().Equal("Invalid phone number format");
    }

    [Fact]
    public void Phone_LongerThanItsColumn_IsRefused()
    {
        // Regression: 21 characters passed the check and failed the save, as a 500
        var result = new UpdateLeadRequestValidator().Validate(Dana(r => r.Phone = "+1 (512) 555-0187 0000"));

        MessagesOf(result, "Phone").Should().Equal("Phone number cannot exceed 20 characters");
    }

    [Fact]
    public void LeadPage_KnownStatuses_Pass_AndAnUnknownOneIsRefused()
    {
        var validator = new LeadPageRequestValidator();

        validator.Validate(new LeadPageRequest { Status = ["Converted", "Disqualified", "Lost"] }).IsValid.Should().BeTrue();
        validator.Validate(new LeadPageRequest { Status = ["New", "Closed"] }).Errors
            .Select(e => e.PropertyName).Should().Equal("Status[1]");
    }

    [Fact]
    public void LeadPage_KeepsThePagingRules()
    {
        new LeadPageRequestValidator().Validate(new LeadPageRequest { PageSize = 101 }).Errors
            .Select(e => e.PropertyName).Should().Equal("PageSize");
    }

    [Fact]
    public void CustomerPage_SearchUpTo100Characters()
    {
        var validator = new CustomerPageRequestValidator();

        validator.Validate(new CustomerPageRequest { Q = new string('a', 100) }).IsValid.Should().BeTrue();
        MessagesOf(validator.Validate(new CustomerPageRequest { Q = new string('a', 101) }), "Q")
            .Should().Equal("Search cannot exceed 100 characters");
    }
}
