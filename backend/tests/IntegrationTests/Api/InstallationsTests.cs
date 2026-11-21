using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Domain.Enums;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using TestUtilities.Database;

namespace IntegrationTests.Api;

// The installation endpoints as the staff app calls them (FR-I1–I7, BR-I1–I3, BR-C1): the office's, and a technician's
// own work. Every installation here starts as the office makes one: a lead from the form, contacted, converted.
public class InstallationsTests(PostgresFixture database) : ApiTestBase(database)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static async Task<JsonElement> JsonAsync(HttpResponseMessage response) =>
        await response.Content.ReadFromJsonAsync<JsonElement>(Ct);

    private static async Task<JsonElement> OkJsonAsync(Task<HttpResponseMessage> call)
    {
        var response = await call;
        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync(Ct));
        return await JsonAsync(response);
    }

    /// <summary>A lead from the form, contacted and converted by the Admin: the new installation's id.</summary>
    private async Task<int> ConvertedAsync(HttpClient admin, string email = "jane.doe@example.com", string street = "100 Congress Ave")
    {
        var lead = await JsonAsync(await CreateClient().PostAsJsonAsync("/api/leads", ErrorResponseTests.ValidLead(email, street), Ct));
        var id = lead.GetProperty("id").GetInt32();
        await OkJsonAsync(admin.PutAsJsonAsync($"/api/leads/{id}/status", new { status = "Contacted" }, Ct));
        var converted = await OkJsonAsync(admin.PostAsync($"/api/leads/{id}/convert", null, Ct));
        return converted.GetProperty("installationId").GetInt32();
    }

    private static Task<JsonElement> Move(HttpClient admin, int id, string to) =>
        OkJsonAsync(admin.PostAsJsonAsync($"/api/installations/{id}/status", new { status = to }, Ct));

    private static Task<JsonElement> Assign(HttpClient admin, int id, int technicianId, string role) =>
        OkJsonAsync(admin.PostAsJsonAsync($"/api/installations/{id}/assignments", new { technicianId, role }, Ct));

    private static int AssignmentId(JsonElement installation, string role) =>
        installation.GetProperty("assignments").EnumerateArray().First(a => a.GetProperty("role").GetString() == role)
            .GetProperty("id").GetInt32();

    [Fact]
    public async Task AnInstallationsWholePath_FromConversionToSwitchOn()
    {
        // Arrange - the Admin, and a technician (created by signing them in)
        var admin = await CreateClientAsAsync(UserRole.Admin);
        var technician = await CreateClientAsAsync(UserRole.Technician);
        var techId = await IdOfAsync(TestTechnician.Email);
        var id = await ConvertedAsync(admin);

        // Survey: the first history row names who converted it; the next move waits for a surveyor
        var page = await OkJsonAsync(admin.GetAsync($"/api/installations/{id}", Ct));
        page.GetProperty("status").GetString().Should().Be("Survey");
        page.GetProperty("next").GetRawText().Should()
            .Be("""{"status":"Design","allowed":false,"reason":"Assign a surveyor, and have them complete the survey."}""");
        page.GetProperty("timeZone").GetRawText().Should().Be("""{"id":"America/Chicago","name":"Central","fixed":false}""");
        page.GetProperty("timeZones").GetArrayLength().Should().Be(8);
        var created = page.GetProperty("history").EnumerateArray().Single();
        (created.GetProperty("kind").GetString(), created.GetProperty("to").GetString(), created.GetProperty("by").GetProperty("name").GetString())
            .Should().Be(("Status", "Survey", TestAdmin.Name));

        // The surveyor completes their own part, from My work
        page = await Assign(admin, id, techId, "Surveyor");
        var mine = await OkJsonAsync(technician.GetAsync("/api/my-work", Ct));
        mine.EnumerateArray().Single().GetProperty("installationId").GetInt32().Should().Be(id);
        var item = await OkJsonAsync(technician.GetAsync($"/api/my-work/{id}", Ct));
        item.GetProperty("customer").GetRawText().Should().Be("""{"name":"Jane Doe","phone":"+1 512 555 0100"}""");
        item = await OkJsonAsync(technician.PostAsync($"/api/my-work/{id}/assignments/{AssignmentId(page, "Surveyor")}/complete", null, Ct));
        item.GetProperty("myAssignments")[0].GetProperty("completedAt").ValueKind.Should().Be(JsonValueKind.String);

        // Design, with its data; Permits; an installer who completes; Inspection; an inspector, completed by the Admin for them
        await Move(admin, id, "Design");
        await OkJsonAsync(admin.PutAsJsonAsync($"/api/installations/{id}/design", new { systemSizeKw = 8.4m, panelCount = 21, inverterType = "Enphase IQ8M" }, Ct));
        await Move(admin, id, "Permits");
        page = await Assign(admin, id, techId, "Installer");
        await Move(admin, id, "Installation");
        await OkJsonAsync(technician.PostAsync($"/api/my-work/{id}/assignments/{AssignmentId(page, "Installer")}/complete", null, Ct));
        await Move(admin, id, "Inspection");
        page = await Assign(admin, id, techId, "Inspector");
        page = await OkJsonAsync(admin.PostAsync($"/api/installations/{id}/assignments/{AssignmentId(page, "Inspector")}/complete", null, Ct));
        page.GetProperty("next").GetProperty("allowed").GetBoolean().Should().BeTrue();

        // Act - switched on
        page = await Move(admin, id, "Active");

        // Assert - the customer is Active, the design and the zone are fixed, the customer's email is waiting to go,
        // and the history has every change, newest first
        page.GetProperty("customer").GetProperty("status").GetString().Should().Be("Active");
        page.GetProperty("activatedAt").ValueKind.Should().Be(JsonValueKind.String);
        page.GetProperty("design").GetProperty("fixed").GetBoolean().Should().BeTrue();
        page.GetProperty("timeZone").GetProperty("fixed").GetBoolean().Should().BeTrue();
        page.GetProperty("next").ValueKind.Should().Be(JsonValueKind.Null);
        (await Context.OutboxMessages.CountAsync(m => m.InstallationId == id && m.Type == OutboxMessageType.InstallationActivated, Ct)).Should().Be(1);
        var history = page.GetProperty("history").EnumerateArray().ToList();
        history.Where(h => h.GetProperty("kind").GetString() == "Status").Select(h => h.GetProperty("to").GetString())
            .Should().Equal("Active", "Inspection", "Installation", "Permits", "Design", "Survey");
        var inspected = history.Single(h => h.GetProperty("kind").GetString() == "Completed" && h.GetProperty("role").GetString() == "Inspector");
        (inspected.GetProperty("technician").GetProperty("name").GetString(), inspected.GetProperty("by").GetProperty("name").GetString())
            .Should().Be((TestTechnician.Name, TestAdmin.Name));
        var assignments = await OkJsonAsync(admin.GetAsync($"/api/users/{techId}/assignments", Ct));
        assignments.GetArrayLength().Should().Be(3);
    }

    [Fact]
    public async Task BR_I1_AMoveItsConditionDoesntAllow_Returns409WithWhatItWaitsFor()
    {
        var admin = await CreateClientAsAsync(UserRole.Admin);
        var id = await ConvertedAsync(admin);

        var skip = await admin.PostAsJsonAsync($"/api/installations/{id}/status", new { status = "Permits" }, Ct);
        var early = await admin.PostAsJsonAsync($"/api/installations/{id}/status", new { status = "Design" }, Ct);

        (await ShouldBeProblemAsync(skip, HttpStatusCode.Conflict, "installation.transition_not_allowed"))
            .GetProperty("detail").GetString().Should().Be("This installation is at Survey: it can only move to Design.");
        (await ShouldBeProblemAsync(early, HttpStatusCode.Conflict, "installation.transition_not_allowed"))
            .GetProperty("detail").GetString().Should().Be("Can't move to Design yet. Assign a surveyor, and have them complete the survey.");
    }

    [Fact]
    public async Task BR_I2_Cancel_NeedsAReason_KeepsItInTheHistory_AndTheCustomerFollows()
    {
        var admin = await CreateClientAsAsync(UserRole.Admin);
        var id = await ConvertedAsync(admin);

        var noReason = await admin.PostAsJsonAsync($"/api/installations/{id}/cancel", new { reason = " " }, Ct);
        var page = await OkJsonAsync(admin.PostAsJsonAsync($"/api/installations/{id}/cancel", new { reason = "The homeowner is selling the house." }, Ct));

        await ShouldBeProblemAsync(noReason, HttpStatusCode.BadRequest, "validation_failed");
        page.GetProperty("status").GetString().Should().Be("Cancelled");
        page.GetProperty("customer").GetProperty("status").GetString().Should().Be("Inactive");
        page.GetProperty("history")[0].GetProperty("note").GetString().Should().Be("The homeowner is selling the house.");
        (page.GetProperty("canCancel").GetBoolean(), page.GetProperty("canDeactivate").GetBoolean()).Should().Be((false, false));
        var again = await admin.PostAsJsonAsync($"/api/installations/{id}/cancel", new { reason = "Twice" }, Ct);
        await ShouldBeProblemAsync(again, HttpStatusCode.Conflict, "installation.transition_not_allowed");
    }

    [Fact]
    public async Task BR_C1_ACustomersSecondInstallation_KeepsThemProspectUntilOneIsActive()
    {
        // The same person converted twice (the lead reuse of BR-L3): one installation cancelled, one in progress
        var admin = await CreateClientAsAsync(UserRole.Admin);
        var first = await ConvertedAsync(admin);
        await OkJsonAsync(admin.PostAsJsonAsync($"/api/installations/{first}/cancel", new { reason = "Changed their mind." }, Ct));
        var second = await ConvertedAsync(admin, street: "2 Second St");

        var page = await OkJsonAsync(admin.GetAsync($"/api/installations/{second}", Ct));

        page.GetProperty("customer").GetProperty("status").GetString().Should().Be("Inactive");
        page = await OkJsonAsync(admin.PutAsJsonAsync($"/api/installations/{second}/time-zone", new { timeZone = "America/Denver" }, Ct));
        page.GetProperty("timeZone").GetProperty("name").GetString().Should().Be("Mountain");
        var cancelled = await OkJsonAsync(admin.PostAsJsonAsync($"/api/installations/{second}/cancel", new { reason = "Again." }, Ct));
        cancelled.GetProperty("customer").GetProperty("status").GetString().Should().Be("Inactive");
    }

    [Fact]
    public async Task FR_I6_ATechnician_SeesOnlyTheirOwn_AndCompletesOnlyTheirPart_AtItsStage()
    {
        var admin = await CreateClientAsAsync(UserRole.Admin);
        var technician = await CreateClientAsAsync(UserRole.Technician);
        var techId = await IdOfAsync(TestTechnician.Email);
        var theirs = await ConvertedAsync(admin);
        var other = await ConvertedAsync(admin, "john.roe@example.com", "2 Other St");
        var page = await Assign(admin, theirs, techId, "Installer");

        var notTheirs = await technician.GetAsync($"/api/my-work/{other}", Ct);
        var tooEarly = await technician.PostAsync($"/api/my-work/{theirs}/assignments/{AssignmentId(page, "Installer")}/complete", null, Ct);
        var otherPage = await Assign(admin, other, techId, "Surveyor");
        var otherPart = await technician.PostAsync($"/api/my-work/{theirs}/assignments/{AssignmentId(otherPage, "Surveyor")}/complete", null, Ct);

        await ShouldBeProblemAsync(notTheirs, HttpStatusCode.NotFound, "installation.not_found");
        (await ShouldBeProblemAsync(tooEarly, HttpStatusCode.Conflict, "assignment.not_at_its_stage"))
            .GetProperty("detail").GetString().Should().Be("The installer's part is completed at Installation; this installation is at Survey.");
        await ShouldBeProblemAsync(otherPart, HttpStatusCode.NotFound, "assignment.not_found");
    }

    [Fact]
    public async Task FR_I5_Assign_AnAdmin_OrTwiceInOneRole_Returns409()
    {
        var admin = await CreateClientAsAsync(UserRole.Admin);
        await CreateClientAsAsync(UserRole.Technician);
        var techId = await IdOfAsync(TestTechnician.Email);
        var id = await ConvertedAsync(admin);
        await Assign(admin, id, techId, "Surveyor");

        var anAdmin = await admin.PostAsJsonAsync($"/api/installations/{id}/assignments", new { technicianId = await IdOfAsync(TestAdmin.Email), role = "Surveyor" }, Ct);
        var nobody = await admin.PostAsJsonAsync($"/api/installations/{id}/assignments", new { technicianId = 999, role = "Surveyor" }, Ct);
        var twice = await admin.PostAsJsonAsync($"/api/installations/{id}/assignments", new { technicianId = techId, role = "Surveyor" }, Ct);

        await ShouldBeProblemAsync(anAdmin, HttpStatusCode.Conflict, "assignment.not_a_technician");
        await ShouldBeProblemAsync(nobody, HttpStatusCode.Conflict, "assignment.not_a_technician");
        await ShouldBeProblemAsync(twice, HttpStatusCode.Conflict, "assignment.duplicate");
    }

    [Fact]
    public async Task Unassign_ANotCompletedPart_Goes()
    {
        var admin = await CreateClientAsAsync(UserRole.Admin);
        await CreateClientAsAsync(UserRole.Technician);
        var id = await ConvertedAsync(admin);
        var page = await Assign(admin, id, await IdOfAsync(TestTechnician.Email), "Installer");

        page = await OkJsonAsync(admin.DeleteAsync($"/api/installations/{id}/assignments/{AssignmentId(page, "Installer")}", Ct));

        page.GetProperty("assignments").GetArrayLength().Should().Be(0);
    }

    [Fact]
    public async Task GetInstallations_ByStatus_LastChangeFirst_AndAnUnknownStatusIs400()
    {
        var admin = await CreateClientAsAsync(UserRole.Admin);
        var first = await ConvertedAsync(admin);
        var second = await ConvertedAsync(admin, "john.roe@example.com", "2 Other St");
        await OkJsonAsync(admin.PostAsJsonAsync($"/api/installations/{first}/cancel", new { reason = "No." }, Ct));

        var all = await OkJsonAsync(admin.GetAsync("/api/installations", Ct));
        var survey = await OkJsonAsync(admin.GetAsync("/api/installations?status=Survey", Ct));
        var unknown = await admin.GetAsync("/api/installations?status=Closed", Ct);

        all.GetProperty("items").EnumerateArray().Select(i => i.GetProperty("id").GetInt32()).Should().Equal(first, second);
        survey.GetProperty("items").EnumerateArray().Select(i => i.GetProperty("id").GetInt32()).Should().Equal(second);
        await ShouldBeProblemAsync(unknown, HttpStatusCode.BadRequest, "validation_failed");
    }

    [Fact]
    public async Task FR_N2_AFailedSwitchOnEmail_IsListedWithItsInstallationAndCustomer()
    {
        var admin = await CreateClientAsAsync(UserRole.Admin);
        var id = await ConvertedAsync(admin);
        var installation = await Context.Installations.Include(i => i.Customer).SingleAsync(i => i.Id == id, Ct);
        await Context.Database.ExecuteSqlAsync($"""
            INSERT INTO outbox_messages (id, type, installation_id, status, attempts, created_at, give_up_at, next_attempt_at, last_attempt_at, last_error)
            VALUES ({Guid.CreateVersion7()}, 'InstallationActivated', {id}, 'Failed', 14, now(), now(), now(), now(), 'The mail server can''t be reached.')
            """, Ct);

        var failed = await OkJsonAsync(admin.GetAsync("/api/notifications/failed", Ct));

        var row = failed.GetProperty("items").EnumerateArray().Single(r => r.GetProperty("type").GetString() == "InstallationActivated");
        row.GetProperty("to").GetRawText().Should().Be($$"""{"name":"Jane Doe","address":"{{installation.Customer.Email}}"}""");
        row.GetProperty("lead").ValueKind.Should().Be(JsonValueKind.Null);
        row.GetProperty("installation").GetRawText().Should().Be($$"""{"id":{{id}},"street":"100 Congress Ave"}""");
    }
}
