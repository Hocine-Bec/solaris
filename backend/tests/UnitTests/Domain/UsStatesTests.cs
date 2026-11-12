using Domain.Common;
using FluentAssertions;

namespace UnitTests.Domain;

public class UsStatesTests
{
    [Fact]
    public void Codes_AreTheFiftyStatesAndDc()
    {
        // Assert
        UsStates.Codes.Should().HaveCount(51).And.Contain("DC").And.OnlyContain(code => code.Length == 2);
    }
}
