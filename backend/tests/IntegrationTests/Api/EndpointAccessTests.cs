using System.Net;
using System.Net.Http.Json;
using Domain.Enums;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using TestUtilities.Database;

namespace IntegrationTests.Api;

// Every endpoint the API maps, and who may call it (ADR 9). The guard: an endpoint open to anyone must be on the list
// of public ones. The matrix: each endpoint called signed out, as a technician and as the Admin, against the table
// below. A new endpoint fails these tests until it is added to the table, so its access is always decided on purpose.
public class EndpointAccessTests(PostgresFixture database) : ApiTestBase(database)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public enum Access { Anyone, Staff, Admin, Technician }

    public enum Caller { SignedOut, Technician, Admin }

    /// <summary>Who may call each endpoint, as "METHOD route" in the API's own route templates.</summary>
    private static readonly Dictionary<string, Access> Table = new()
    {
        ["POST api/auth/login"] = Access.Anyone,
        ["POST api/auth/logout"] = Access.Anyone,
        ["GET api/auth/me"] = Access.Staff,
        ["PUT api/auth/password"] = Access.Staff,
        ["POST api/leads"] = Access.Anyone,   // the website's quote form
        ["GET api/leads"] = Access.Admin,
        ["GET api/leads/{id:int}"] = Access.Admin,
        ["PUT api/leads/{id:int}"] = Access.Admin,
        ["PUT api/leads/{id:int}/status"] = Access.Admin,
        ["POST api/leads/{id:int}/convert"] = Access.Admin,
        ["GET api/customers"] = Access.Admin,
        ["GET api/customers/{id:int}"] = Access.Admin,
        ["GET api/installations"] = Access.Admin,
        ["GET api/installations/{id:int}"] = Access.Admin,
        ["POST api/installations/{id:int}/status"] = Access.Admin,
        ["POST api/installations/{id:int}/cancel"] = Access.Admin,
        ["POST api/installations/{id:int}/deactivate"] = Access.Admin,
        ["PUT api/installations/{id:int}/design"] = Access.Admin,
        ["PUT api/installations/{id:int}/time-zone"] = Access.Admin,
        ["POST api/installations/{id:int}/assignments"] = Access.Admin,
        ["DELETE api/installations/{id:int}/assignments/{assignmentId:int}"] = Access.Admin,
        ["POST api/installations/{id:int}/assignments/{assignmentId:int}/complete"] = Access.Admin,
        ["POST api/installations/{id:int}/device-key"] = Access.Admin,
        ["GET api/my-work"] = Access.Technician,   // a technician's own work
        ["GET api/my-work/{installationId:int}"] = Access.Technician,
        ["POST api/my-work/{installationId:int}/assignments/{assignmentId:int}/complete"] = Access.Technician,
        ["GET api/notifications/failed"] = Access.Admin,
        ["POST api/notifications/{id:guid}/retry"] = Access.Admin,
        ["POST api/notifications/failed/retry"] = Access.Admin,
        ["GET api/users"] = Access.Admin,
        ["GET api/users/{id:int}"] = Access.Admin,
        ["POST api/users"] = Access.Admin,
        ["PUT api/users/{id:int}"] = Access.Admin,
        ["PUT api/users/{id:int}/password"] = Access.Admin,
        ["POST api/users/{id:int}/deactivate"] = Access.Admin,
        ["POST api/users/{id:int}/reactivate"] = Access.Admin,
        ["GET api/users/{id:int}/assignments"] = Access.Admin,
    };

    /// <summary>
    /// Open to anyone, and mapped, in Development only: the API reference, the measurement harness, and the background
    /// jobs' dashboard (which answers requests from the same machine only).
    /// </summary>
    private static readonly string[] DevelopmentOnly =
    [
        "ANY hangfire/{**path}",
        "GET openapi/{documentName}.json",
        "GET scalar/{documentName?}",
        "GET scalar/scalar.js",
        "GET scalar/scalar.aspnetcore.js",
        "GET api/dev/email-fault/",
        "PUT api/dev/email-fault/",
    ];

    private static IEnumerable<string> Public => Table.Where(e => e.Value == Access.Anyone).Select(e => e.Key);

    /// <summary>The endpoints the running API maps, as "METHOD route"; with anonymousOnly, those open to anyone.</summary>
    private List<string> Mapped(bool anonymousOnly = false) =>
        ApiServices.GetRequiredService<EndpointDataSource>().Endpoints.OfType<RouteEndpoint>()
            .Where(e => !anonymousOnly || e.Metadata.GetMetadata<IAllowAnonymous>() is not null)
            .SelectMany(e => (e.Metadata.GetMetadata<IHttpMethodMetadata>()?.HttpMethods ?? ["ANY"])
                .Select(method => $"{method} {e.RoutePattern.RawText?.TrimStart('/')}"))
            .Order()
            .ToList();

    #region The guard

    [Fact]
    public void EveryEndpoint_IsInTheTable()
    {
        CreateClient();

        Mapped().Should().BeEquivalentTo(Table.Keys,
            "every endpoint's access is decided on purpose: add a new endpoint to the table");
    }

    [Fact]
    public void EndpointsOpenToAnyone_AreOnlyThePublicOnes()
    {
        CreateClient();

        Mapped(anonymousOnly: true).Should().BeEquivalentTo(Public);
    }

    [Fact]
    public void EndpointsOpenToAnyone_InDevelopment_AddOnlyTheApiReferenceTheHarnessAndTheJobsDashboard()
    {
        CreateClient(environment: "Development");

        Mapped(anonymousOnly: true).Should().BeEquivalentTo(Public.Concat(DevelopmentOnly));
    }

    #endregion

    #region The matrix

    public static TheoryData<string, Caller> Calls()
    {
        var calls = new TheoryData<string, Caller>();
        foreach (var endpoint in Table.Keys)
        foreach (var caller in Enum.GetValues<Caller>())
            calls.Add(endpoint, caller);
        return calls;
    }

    [Theory]
    [MemberData(nameof(Calls))]
    public async Task Endpoint_AnswersEachCallerAsTheTableSays(string endpoint, Caller caller)
    {
        // Arrange - an empty body where there is one: an allowed caller gets past authorization to validation
        var client = caller switch
        {
            Caller.Technician => await CreateClientAsAsync(UserRole.Technician),
            Caller.Admin => await CreateClientAsAsync(UserRole.Admin),
            _ => CreateClient()
        };
        var method = endpoint.Split(' ')[0];
        var url = "/" + System.Text.RegularExpressions.Regex.Replace(endpoint.Split(' ')[1], @"\{\w+:int\}", "1")
            .Replace("{id:guid}", Guid.Empty.ToString());
        var request = new HttpRequestMessage(new HttpMethod(method), url)
        {
            Content = method is "POST" or "PUT" ? JsonContent.Create(new { }) : null
        };
        var allowed = Table[endpoint] switch
        {
            Access.Anyone => true,
            Access.Staff => caller != Caller.SignedOut,
            Access.Technician => caller == Caller.Technician,
            _ => caller == Caller.Admin
        };

        // Act
        var response = await client.SendAsync(request, Ct);

        // Assert
        if (allowed)
            new[] { HttpStatusCode.Unauthorized, HttpStatusCode.Forbidden }.Should()
                .NotContain(response.StatusCode, $"{caller} may call {endpoint}");
        else if (caller == Caller.SignedOut)
            await ShouldBeProblemAsync(response, HttpStatusCode.Unauthorized, "unauthorized");
        else
            await ShouldBeProblemAsync(response, HttpStatusCode.Forbidden, "forbidden");
    }

    #endregion
}
