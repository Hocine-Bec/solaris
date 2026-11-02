using System.Reflection;
using Application.Common.Models;
using Domain.Entities;
using FluentAssertions;

namespace UnitTests.Architecture;

// ADR 1: Domain and Application must not depend on the database, the job runner or the mail library.
public class LayerDependencyTests
{
    private static readonly string[] ForbiddenPrefixes =
        ["Microsoft.EntityFrameworkCore", "Npgsql", "Hangfire", "MailKit"];

    private static readonly Dictionary<string, Assembly> Layers = new()
    {
        ["Domain"] = typeof(Lead).Assembly,
        ["Application"] = typeof(Result).Assembly,
    };

    [Theory]
    [InlineData("Domain")]
    [InlineData("Application")]
    public void Layer_References_ShouldNotIncludeInfrastructureLibraries(string layer)
    {
        // Arrange
        var assembly = Layers[layer];

        // Act
        var forbidden = assembly.GetReferencedAssemblies()
            .Select(reference => reference.Name!)
            .Where(name => ForbiddenPrefixes.Any(prefix => name.StartsWith(prefix, StringComparison.Ordinal)))
            .ToList();

        // Assert
        forbidden.Should().BeEmpty("{0} must not depend on EF Core, Npgsql, Hangfire or MailKit (ADR 1)", layer);
    }
}
