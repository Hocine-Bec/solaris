using System.Net;
using System.Net.Http.Json;
using Application.DTOs.Common;
using Application.DTOs.Lead;
using Domain.Entities;
using Domain.Enums;
using FluentAssertions;
using TestUtilities.Database;
using TestUtilities.Factories;

namespace IntegrationTests.Api;

// Every list takes ?page=1&pageSize=20 (at most 100) and returns { items, page, pageSize, totalCount }.
public class PaginationTests(PostgresFixture database) : ApiTestBase(database)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task GetLeads_NoQuery_ReturnsTheFirstPageOf20NewestFirst()
    {
        await SeedLeadsAsync(25);

        var page = await (await CreateClientAsAsync(UserRole.Admin)).GetFromJsonAsync<PagedResponse<LeadResponse>>("/api/leads", Ct);

        page!.Page.Should().Be(1);
        page.PageSize.Should().Be(20);
        page.TotalCount.Should().Be(25);
        page.Items.Select(l => l.Id).Should().Equal(Enumerable.Range(6, 20).Reverse());
    }

    [Fact]
    public async Task GetLeads_LastPage_ReturnsTheRemainingLeads()
    {
        await SeedLeadsAsync(25);

        var page = await (await CreateClientAsAsync(UserRole.Admin)).GetFromJsonAsync<PagedResponse<LeadResponse>>(
            "/api/leads?page=2&pageSize=20", Ct);

        page!.Items.Select(l => l.Id).Should().Equal(5, 4, 3, 2, 1);
        page.TotalCount.Should().Be(25);
    }

    [Fact]
    public async Task GetLeads_PageAfterTheEnd_ReturnsNoItemsAndTheTotal()
    {
        await SeedLeadsAsync(3);

        var page = await (await CreateClientAsAsync(UserRole.Admin)).GetFromJsonAsync<PagedResponse<LeadResponse>>("/api/leads?page=5", Ct);

        page!.Items.Should().BeEmpty();
        page.TotalCount.Should().Be(3);
    }

    [Theory]
    [InlineData("page=0", "Page")]
    [InlineData("pageSize=0", "PageSize")]
    [InlineData("pageSize=101", "PageSize")]
    [InlineData("page=2147483647&pageSize=100", "Page")]
    public async Task GetLeads_PageOutOfRange_Returns400ValidationFailed(string query, string field)
    {
        var response = await (await CreateClientAsAsync(UserRole.Admin)).GetAsync($"/api/leads?{query}", Ct);

        var body = await ShouldBeProblemAsync(response, HttpStatusCode.BadRequest, "validation_failed");
        body.GetProperty("errors").TryGetProperty(field, out _).Should().BeTrue();
    }

    private async Task SeedLeadsAsync(int count)
    {
        await SeedAsync(AddressFactory.CreateFakeAddress(id: 1));
        await SeedAsync(Enumerable.Range(1, count)
            .Select(i => LeadFactory.CreateFakeLead(id: i, addressId: 1))
            .ToArray<Lead>());
    }
}
