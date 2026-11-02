using Domain.Entities;
using FluentAssertions;

namespace UnitTests.Domain;

public class AddressTests
{
    [Fact]
    public void BuildHash_SameInputs_ProducesSameHash()
    {
        // Arrange
        var street = "123 Main St";
        var city = "Springfield";
        var state = "IL";
        var zip = "62701";
        var country = "US";

        // Act
        var hash1 = Address.BuildHash(street, city, state, zip, country);
        var hash2 = Address.BuildHash(street, city, state, zip, country);

        // Assert
        hash1.Should().Be(hash2);
    }

    [Fact]
    public void BuildHash_CaseInsensitive()
    {
        // Arrange & Act
        var hash1 = Address.BuildHash("Main St", "Springfield", "IL", "62701", "US");
        var hash2 = Address.BuildHash("main st", "springfield", "il", "62701", "us");

        // Assert
        hash1.Should().Be(hash2);
    }
}