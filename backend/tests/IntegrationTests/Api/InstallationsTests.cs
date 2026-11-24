using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Application.Common.Security;
using Domain.Enums;
using Infrastructure.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TestUtilities.Database;

namespace IntegrationTests.Api;

// The installation endpoints as the staff app calls them (FR-I1–I7, BR-I1–I3, BR-C1): the office's, and a technician's
// own work. Every installation here starts as the office makes one: a lead from the form, contacted, converted.
public class InstallationsTests(PostgresFixture database) : ApiTestBase(database)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

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
        item = await OkJsonAsync(Change(technician, HttpMethod.Post, $"/api/my-work/{id}/assignments/{AssignmentId(page, "Surveyor")}/complete"));
        item.GetProperty("myAssignments")[0].GetProperty("completedAt").ValueKind.Should().Be(JsonValueKind.String);

        // Design, with its data; Permits; an installer who completes; Inspection; an inspector, completed by the Admin for them
        await Move(admin, id, "Design");
        await OkJsonAsync(Change(admin, HttpMethod.Put, $"/api/installations/{id}/design", new { systemSizeKw = 8.4m, panelCount = 21, inverterType = "Enphase IQ8M" }));
        await Move(admin, id, "Permits");
        page = await Assign(admin, id, techId, "Installer");
        await Move(admin, id, "Installation");
        await OkJsonAsync(Change(technician, HttpMethod.Post, $"/api/my-work/{id}/assignments/{AssignmentId(page, "Installer")}/complete"));
        await Move(admin, id, "Inspection");
        page = await Assign(admin, id, techId, "Inspector");
        page = await OkJsonAsync(Change(admin, HttpMethod.Post, $"/api/installations/{id}/assignments/{AssignmentId(page, "Inspector")}/complete"));
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

        var skip = await Change(admin, HttpMethod.Post, $"/api/installations/{id}/status", new { status = "Permits" });
        var early = await Change(admin, HttpMethod.Post, $"/api/installations/{id}/status", new { status = "Design" });

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

        var noReason = await Change(admin, HttpMethod.Post, $"/api/installations/{id}/cancel", new { reason = " " });
        var page = await OkJsonAsync(Change(admin, HttpMethod.Post, $"/api/installations/{id}/cancel", new { reason = "The homeowner is selling the house." }));

        await ShouldBeProblemAsync(noReason, HttpStatusCode.BadRequest, "validation_failed");
        page.GetProperty("status").GetString().Should().Be("Cancelled");
        page.GetProperty("customer").GetProperty("status").GetString().Should().Be("Inactive");
        page.GetProperty("history")[0].GetProperty("note").GetString().Should().Be("The homeowner is selling the house.");
        (page.GetProperty("canCancel").GetBoolean(), page.GetProperty("canDeactivate").GetBoolean()).Should().Be((false, false));
        var again = await Change(admin, HttpMethod.Post, $"/api/installations/{id}/cancel", new { reason = "Twice" });
        await ShouldBeProblemAsync(again, HttpStatusCode.Conflict, "installation.transition_not_allowed");
    }

    [Fact]
    public async Task BR_C1_ACustomersSecondInstallation_KeepsThemProspectUntilOneIsActive()
    {
        // The same person converted twice (the lead reuse of BR-L3): one installation cancelled, one in progress
        var admin = await CreateClientAsAsync(UserRole.Admin);
        var first = await ConvertedAsync(admin);
        await OkJsonAsync(Change(admin, HttpMethod.Post, $"/api/installations/{first}/cancel", new { reason = "Changed their mind." }));
        var second = await ConvertedAsync(admin, street: "2 Second St");

        var page = await OkJsonAsync(admin.GetAsync($"/api/installations/{second}", Ct));

        page.GetProperty("customer").GetProperty("status").GetString().Should().Be("Inactive");
        page = await OkJsonAsync(Change(admin, HttpMethod.Put, $"/api/installations/{second}/time-zone", new { timeZone = "America/Denver" }));
        page.GetProperty("timeZone").GetProperty("name").GetString().Should().Be("Mountain");
        var cancelled = await OkJsonAsync(Change(admin, HttpMethod.Post, $"/api/installations/{second}/cancel", new { reason = "Again." }));
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
        var tooEarly = await Change(technician, HttpMethod.Post, $"/api/my-work/{theirs}/assignments/{AssignmentId(page, "Installer")}/complete");
        var otherPage = await Assign(admin, other, techId, "Surveyor");
        var otherPart = await Change(technician, HttpMethod.Post, $"/api/my-work/{theirs}/assignments/{AssignmentId(otherPage, "Surveyor")}/complete");

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

        var anAdmin = await Change(admin, HttpMethod.Post, $"/api/installations/{id}/assignments", new { technicianId = await IdOfAsync(TestAdmin.Email), role = "Surveyor" });
        var nobody = await Change(admin, HttpMethod.Post, $"/api/installations/{id}/assignments", new { technicianId = 999, role = "Surveyor" });
        var twice = await Change(admin, HttpMethod.Post, $"/api/installations/{id}/assignments", new { technicianId = techId, role = "Surveyor" });

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

        page = await OkJsonAsync(Change(admin, HttpMethod.Delete, $"/api/installations/{id}/assignments/{AssignmentId(page, "Installer")}"));

        page.GetProperty("assignments").GetArrayLength().Should().Be(0);
    }

    [Fact]
    public async Task BR_I4_AChange_NeedsTheVersionItWasMadeOn()
    {
        // Arrange - an installation and its version, as the page read it
        var admin = await CreateClientAsAsync(UserRole.Admin);
        var technician = await CreateClientAsAsync(UserRole.Technician);
        var id = await ConvertedAsync(admin);
        var read = (await admin.GetAsync($"/api/installations/{id}", Ct)).Headers.ETag!.Tag;
        var design = new { systemSizeKw = 8.4m, panelCount = 21, inverterType = "Enphase IQ8M" };

        // Act - a change on that version; then, on the same old one, an assignment and a move; without a version;
        // with something that isn't one
        var saved = await Send(admin, HttpMethod.Put, $"/api/installations/{id}/design", design, read);
        var assigned = await Send(admin, HttpMethod.Post, $"/api/installations/{id}/assignments",
            new { technicianId = await IdOfAsync(TestTechnician.Email), role = "Surveyor" }, read);
        var moved = await Send(admin, HttpMethod.Post, $"/api/installations/{id}/cancel", new { reason = "Old page." }, read);
        var none = await Send(admin, HttpMethod.Post, $"/api/installations/{id}/cancel", new { reason = "No version." }, null);
        var garbled = await Send(admin, HttpMethod.Post, $"/api/installations/{id}/cancel", new { reason = "Garbled." }, "*");

        // Assert - the first is saved and answers its new version; the others change nothing
        saved.StatusCode.Should().Be(HttpStatusCode.OK);
        saved.Headers.ETag!.Tag.Should().NotBe(read);
        (await ShouldBeProblemAsync(assigned, HttpStatusCode.Conflict, "installation.concurrent_change"))
            .GetProperty("detail").GetString().Should().Be("This installation was changed by someone else. Reload and try again.");
        await ShouldBeProblemAsync(moved, HttpStatusCode.Conflict, "installation.concurrent_change");
        await ShouldBeProblemAsync(none, (HttpStatusCode)428, "installation.version_required");
        await ShouldBeProblemAsync(garbled, HttpStatusCode.Conflict, "installation.concurrent_change");
        var page = await OkJsonAsync(admin.GetAsync($"/api/installations/{id}", Ct));
        (page.GetProperty("status").GetString(), page.GetProperty("assignments").GetArrayLength()).Should().Be(("Survey", 0));
        var techNone = await Send(technician, HttpMethod.Post, $"/api/my-work/{id}/assignments/1/complete", null, null);
        await ShouldBeProblemAsync(techNone, (HttpStatusCode)428, "installation.version_required");
    }

    [Fact]
    public async Task BR_I4_AnAssignmentOnly_MovesTheVersionToo()
    {
        // Arrange - the page's version before an assignment
        var admin = await CreateClientAsAsync(UserRole.Admin);
        await CreateClientAsAsync(UserRole.Technician);
        var id = await ConvertedAsync(admin);
        var before = (await admin.GetAsync($"/api/installations/{id}", Ct)).Headers.ETag!.Tag;

        // Act - assigned on it, then a cancel from a page still on it
        var assigned = await Send(admin, HttpMethod.Post, $"/api/installations/{id}/assignments",
            new { technicianId = await IdOfAsync(TestTechnician.Email), role = "Surveyor" }, before);
        var cancelled = await Send(admin, HttpMethod.Post, $"/api/installations/{id}/cancel", new { reason = "Old page." }, before);

        // Assert
        assigned.StatusCode.Should().Be(HttpStatusCode.OK);
        assigned.Headers.ETag!.Tag.Should().NotBe(before);
        (await admin.GetAsync($"/api/installations/{id}", Ct)).Headers.ETag!.Tag.Should().Be(assigned.Headers.ETag.Tag);
        await ShouldBeProblemAsync(cancelled, HttpStatusCode.Conflict, "installation.concurrent_change");
    }

    [Fact]
    public async Task BR_I4_ATechnicianCompletingOnAnOldVersion_Is409_AndTheirPartStaysOpen()
    {
        // Arrange - the surveyor's page read; then the office changes the installation's time zone
        var admin = await CreateClientAsAsync(UserRole.Admin);
        var technician = await CreateClientAsAsync(UserRole.Technician);
        var id = await ConvertedAsync(admin);
        var page = await Assign(admin, id, await IdOfAsync(TestTechnician.Email), "Surveyor");
        var read = (await technician.GetAsync($"/api/my-work/{id}", Ct)).Headers.ETag!.Tag;
        await OkJsonAsync(Change(admin, HttpMethod.Put, $"/api/installations/{id}/time-zone", new { timeZone = "America/Denver" }));

        // Act
        var completed = await Send(technician, HttpMethod.Post, $"/api/my-work/{id}/assignments/{AssignmentId(page, "Surveyor")}/complete", null, read);

        // Assert
        await ShouldBeProblemAsync(completed, HttpStatusCode.Conflict, "installation.concurrent_change");
        var item = await OkJsonAsync(technician.GetAsync($"/api/my-work/{id}", Ct));
        item.GetProperty("myAssignments")[0].GetProperty("completedAt").ValueKind.Should().Be(JsonValueKind.Null);
    }

    [Fact]
    public async Task BR_I4_ConflictingChangesAtOnce_OneIsSaved_TheOthersAre409()
    {
        // Arrange - an installation ready to move to Design, and ten Admins on the same version of its page: five move
        // it on, five cancel it
        var admin = await CreateClientAsAsync(UserRole.Admin);
        var technician = await CreateClientAsAsync(UserRole.Technician);
        var id = await ConvertedAsync(admin);
        var page = await Assign(admin, id, await IdOfAsync(TestTechnician.Email), "Surveyor");
        await OkJsonAsync(Change(technician, HttpMethod.Post, $"/api/my-work/{id}/assignments/{AssignmentId(page, "Surveyor")}/complete"));
        var version = (await admin.GetAsync($"/api/installations/{id}", Ct)).Headers.ETag!.Tag;
        using var start = new SemaphoreSlim(0);
        var changes = Enumerable.Range(0, 10).Select(i => Task.Run(async () =>
        {
            await start.WaitAsync(Ct);
            return i % 2 == 0
                ? await Send(admin, HttpMethod.Post, $"/api/installations/{id}/status", new { status = "Design" }, version)
                : await Send(admin, HttpMethod.Post, $"/api/installations/{id}/cancel", new { reason = $"Admin {i}." }, version);
        }, Ct)).ToList();

        // Act - released together
        start.Release(changes.Count);
        var responses = await Task.WhenAll(changes);

        // Assert - one saved; nine refused as concurrent; the history has that one change only, and the installation
        // is as its answer says
        var winner = responses.Single(r => r.StatusCode == HttpStatusCode.OK);
        foreach (var refused in responses.Where(r => r != winner))
            await ShouldBeProblemAsync(refused, HttpStatusCode.Conflict, "installation.concurrent_change");
        var answered = (await JsonAsync(winner)).GetProperty("status").GetString();
        var stored = await Context.Installations.Include(i => i.History).SingleAsync(i => i.Id == id, Ct);
        stored.Status.ToString().Should().Be(answered);
        stored.History.Select(h => h.ToStatus.ToString()).Should().BeEquivalentTo(["Survey", answered]);
    }

    #region Device keys (BR-T2)

    /// <summary>The device scheme on its own, as an endpoint whose policy names it runs it.</summary>
    private async Task<AuthenticateResult> AuthenticateDeviceAsync(string? key)
    {
        using var scope = ApiServices.CreateScope();
        var context = new DefaultHttpContext { RequestServices = scope.ServiceProvider };
        if (key is not null)
            context.Request.Headers[DeviceKeyAuthentication.Header] = key;
        return await context.AuthenticateAsync(DeviceKeyAuthentication.Scheme);
    }

    private static string? IssuedKey(JsonElement page) =>
        page.TryGetProperty("issuedKey", out var key) ? key.GetString() : null;

    [Fact]
    public async Task BR_T2_SwitchOn_AnswersTheDeviceKeyOnce_AndKeepsOnlyItsHash()
    {
        // Act
        var (admin, id, switchedOn) = await SwitchedOnAsync();

        // Assert - the answer carries the key, never stored by a browser or a proxy
        switchedOn.StatusCode.Should().Be(HttpStatusCode.OK);
        switchedOn.Headers.CacheControl!.NoStore.Should().BeTrue();
        var answer = await JsonAsync(switchedOn);
        var key = IssuedKey(answer)!;
        key.Should().MatchRegex("^[a-z2-7]{12}\\.[A-Za-z0-9_-]{43}$");
        answer.GetProperty("deviceKey").GetProperty("prefix").GetString().Should().Be(key[..12]);

        // ...the page read later doesn't
        var page = await OkJsonAsync(admin.GetAsync($"/api/installations/{id}", Ct));
        IssuedKey(page).Should().BeNull();
        page.GetProperty("deviceKey").GetProperty("prefix").GetString().Should().Be(key[..12]);

        // ...the database holds the prefix and the secret's hash, not the secret
        var stored = await Context.Installations.AsNoTracking().SingleAsync(i => i.Id == id, Ct);
        stored.DeviceKeyPrefix.Should().Be(key[..12]);
        stored.DeviceKeyHash.Should().Equal(SHA256.HashData(Encoding.ASCII.GetBytes(key[13..])));

        // ...and the key proves the installation
        var device = await AuthenticateDeviceAsync(key);
        device.Succeeded.Should().BeTrue();
        device.Principal!.FindFirst(DeviceKeyAuthentication.InstallationIdClaim)!.Value.Should().Be(id.ToString(CultureInfo.InvariantCulture));
    }

    [Fact]
    public async Task BR_T2_ReplacingTheKey_TheOldOneStopsAtOnce()
    {
        // Arrange
        var (admin, id, switchedOn) = await SwitchedOnAsync();
        var old = IssuedKey(await JsonAsync(switchedOn))!;
        var survey = await ConvertedAsync(admin, "john.roe@example.com", "2 Other St");

        // Act
        var replaced = await Change(admin, HttpMethod.Post, $"/api/installations/{id}/device-key");
        var noVersion = await Send(admin, HttpMethod.Post, $"/api/installations/{id}/device-key", null, null);
        var notActive = await Change(admin, HttpMethod.Post, $"/api/installations/{survey}/device-key");

        // Assert
        replaced.StatusCode.Should().Be(HttpStatusCode.OK);
        replaced.Headers.CacheControl!.NoStore.Should().BeTrue();
        var key = IssuedKey(await JsonAsync(replaced))!;
        key[..12].Should().NotBe(old[..12]);
        (await AuthenticateDeviceAsync(old)).Succeeded.Should().BeFalse();
        (await AuthenticateDeviceAsync(key)).Succeeded.Should().BeTrue();
        await ShouldBeProblemAsync(noVersion, (HttpStatusCode)428, "installation.version_required");
        (await ShouldBeProblemAsync(notActive, HttpStatusCode.Conflict, "installation.not_active"))
            .GetProperty("detail").GetString().Should().Be("This installation is Survey: only an Active one has a device key.");
    }

    [Fact]
    public async Task BR_T2_Deactivating_DeletesTheKey()
    {
        // Arrange
        var (admin, id, switchedOn) = await SwitchedOnAsync();
        var key = IssuedKey(await JsonAsync(switchedOn))!;

        // Act
        var page = await OkJsonAsync(Change(admin, HttpMethod.Post, $"/api/installations/{id}/deactivate", new { note = "Removed with the roof." }));

        // Assert
        page.GetProperty("deviceKey").ValueKind.Should().Be(JsonValueKind.Null);
        (await AuthenticateDeviceAsync(key)).Succeeded.Should().BeFalse();
        (await Context.Installations.AsNoTracking().SingleAsync(i => i.Id == id, Ct)).DeviceKeyHash.Should().BeNull();
    }

    [Fact]
    public async Task BR_T2_EveryKeyThatDoesntWork_IsRefusedTheSameWay()
    {
        // Arrange - a working key, to alter
        var (_, _, switchedOn) = await SwitchedOnAsync();
        var key = IssuedKey(await JsonAsync(switchedOn))!;
        var other = DeviceKeys.Create().Text;

        // Act
        var none = await AuthenticateDeviceAsync(null);
        var refused = await Task.WhenAll(new[]
        {
            "not a key",
            key.ToUpperInvariant(),
            other,                               // well written, an unknown prefix
            key[..13] + other[13..],             // its prefix, another secret
        }.Select(AuthenticateDeviceAsync));

        // Assert - nothing sent: no device; anything else: refused. The answer is one 401, whatever the reason.
        none.None.Should().BeTrue();
        refused.Should().OnlyContain(r => !r.Succeeded && r.Failure != null);
        using var scope = ApiServices.CreateScope();
        var context = new DefaultHttpContext { RequestServices = scope.ServiceProvider, Response = { Body = new MemoryStream() } };
        await context.ChallengeAsync(DeviceKeyAuthentication.Scheme);
        context.Response.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
        context.Response.Body.Position = 0;
        var problem = await JsonSerializer.DeserializeAsync<JsonElement>(context.Response.Body, cancellationToken: Ct);
        problem.GetProperty("code").GetString().Should().Be("device.invalid_key");
    }

    [Fact]
    public async Task BR_T2_AKeyLeftOnAnInstallationNoLongerActive_IsRefused()
    {
        // Arrange - a state the API never leaves (deactivating deletes the key), made directly: the key's read checks
        // the status itself, as a second guard
        var (_, id, switchedOn) = await SwitchedOnAsync();
        var key = IssuedKey(await JsonAsync(switchedOn))!;
        await Context.Database.ExecuteSqlAsync($"UPDATE installations SET status = 'Deactivated' WHERE id = {id}", Ct);

        // Act
        var device = await AuthenticateDeviceAsync(key);

        // Assert
        device.Succeeded.Should().BeFalse();
    }

    [Fact]
    public async Task BR_T2_ADeviceKey_OpensNoStaffEndpoint()
    {
        // Arrange - a working key, sent alone to the office's pages
        var (_, id, switchedOn) = await SwitchedOnAsync();
        var key = IssuedKey(await JsonAsync(switchedOn))!;
        var device = CreateClient();
        device.DefaultRequestHeaders.Add(DeviceKeyAuthentication.Header, key);

        // Act
        var page = await device.GetAsync($"/api/installations/{id}", Ct);
        var mine = await device.GetAsync("/api/my-work", Ct);

        // Assert
        await ShouldBeProblemAsync(page, HttpStatusCode.Unauthorized, "unauthorized");
        await ShouldBeProblemAsync(mine, HttpStatusCode.Unauthorized, "unauthorized");
    }

    #endregion

    [Fact]
    public async Task GetInstallations_ByStatus_LastChangeFirst_AndAnUnknownStatusIs400()
    {
        var admin = await CreateClientAsAsync(UserRole.Admin);
        var first = await ConvertedAsync(admin);
        var second = await ConvertedAsync(admin, "john.roe@example.com", "2 Other St");
        await OkJsonAsync(Change(admin, HttpMethod.Post, $"/api/installations/{first}/cancel", new { reason = "No." }));

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
