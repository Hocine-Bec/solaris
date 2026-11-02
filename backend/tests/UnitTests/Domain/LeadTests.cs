using Domain.Entities;
using Domain.Enums;
using FluentAssertions;

namespace UnitTests.Domain;

public class LeadTests
{
    [Fact]
    public void Lead_DefaultStatus_ShouldBeNew()
    {
        // Arrange & Act
        var lead = new Lead
        {
            FirstName = "John",
            LastName = "Doe",
            Email = "john.doe@example.com",
            PhoneNumber = "123-456-7890"
        };

        // Assert
        lead.Status.Should().Be(LeadStatus.New);
    }

    [Fact]
    public void Lead_CreatedAt_ShouldBeSet()
    {
        // Arrange
        var now = DateTime.UtcNow;
        
        // Act
        var lead = new Lead
        {
            CreatedAt = now
        };

        // Assert
        lead.CreatedAt.Should().BeCloseTo(now, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Lead_ContactedAt_ShouldBeNullByDefault()
    {
        // Arrange & Act
        var lead = new Lead();

        // Assert
        lead.ContactedAt.Should().BeNull();
    }

    [Fact]
    public void Lead_ConvertedAt_ShouldBeNullByDefault()
    {
        // Arrange & Act
        var lead = new Lead();

        // Assert
        lead.ConvertedAt.Should().BeNull();
    }

    [Fact]
    public void Lead_CustomerId_ShouldBeNullByDefault()
    {
        // Arrange & Act
        var lead = new Lead();

        // Assert
        lead.CustomerId.Should().BeNull();
    }

    [Fact]
    public void Lead_AllProperties_ShouldBeSettable()
    {
        // Arrange
        var createdAt = DateTime.UtcNow;
        var contactedAt = DateTime.UtcNow.AddDays(1);
        var convertedAt = DateTime.UtcNow.AddDays(2);

        // Act
        var lead = new Lead
        {
            Id = 1,
            FirstName = "Jane",
            LastName = "Smith",
            Email = "jane.smith@example.com",
            PhoneNumber = "987-654-3210",
            AddressId = 100,
            PropertyType = PropertyType.House,
            IsPropertyOwner = true,
            Status = LeadStatus.Contacted,
            CreatedAt = createdAt,
            ContactedAt = contactedAt,
            ConvertedAt = convertedAt,
            MonthlyBillRange = "$200-$300",
            BestTimeToContact = "Morning",
            Notes = "Interested in solar panels",
            CustomerId = 50
        };

        // Assert
        lead.Id.Should().Be(1);
        lead.FirstName.Should().Be("Jane");
        lead.LastName.Should().Be("Smith");
        lead.Email.Should().Be("jane.smith@example.com");
        lead.PhoneNumber.Should().Be("987-654-3210");
        lead.AddressId.Should().Be(100);
        lead.PropertyType.Should().Be(PropertyType.House);
        lead.IsPropertyOwner.Should().BeTrue();
        lead.Status.Should().Be(LeadStatus.Contacted);
        lead.CreatedAt.Should().Be(createdAt);
        lead.ContactedAt.Should().Be(contactedAt);
        lead.ConvertedAt.Should().Be(convertedAt);
        lead.MonthlyBillRange.Should().Be("$200-$300");
        lead.BestTimeToContact.Should().Be("Morning");
        lead.Notes.Should().Be("Interested in solar panels");
        lead.CustomerId.Should().Be(50);
    }
}
