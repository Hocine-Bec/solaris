using Domain.Entities;
using Domain.Enums;
using FluentAssertions;

namespace UnitTests.Domain;

public class InstallationTests
{
    private static readonly DateTime Now = new(2026, 11, 21, 15, 0, 0, DateTimeKind.Utc);
    private const int Admin = 1;
    private static readonly User Jamal = new() { Id = 7, Name = "Jamal Ortiz", Role = UserRole.Technician, IsActive = true };
    private static readonly User Lena = new() { Id = 8, Name = "Lena Park", Role = UserRole.Technician, IsActive = true };

    private static readonly InstallationStatus[] Forward =
    [
        InstallationStatus.Survey, InstallationStatus.Design, InstallationStatus.Permits, InstallationStatus.Installation,
        InstallationStatus.Inspection, InstallationStatus.Active
    ];

    private static InstallationAssignment Part(User technician, AssignmentRole role, bool done) => new()
    {
        Id = technician.Id * 10 + (int)role, TechnicianId = technician.Id, Technician = technician, Role = role,
        AssignedAt = Now.AddDays(-9), CompletedAt = done ? Now.AddDays(-1) : null
    };

    /// <summary>An installation at <paramref name="status"/> whose every condition is met: the next move is allowed.</summary>
    private static Installation Ready(InstallationStatus status) => new()
    {
        Id = 41, Status = status, TimeZone = "America/Chicago", SystemSizeKw = 8.4m, PanelCount = 21,
        Assignments = [Part(Jamal, AssignmentRole.Surveyor, true), Part(Lena, AssignmentRole.Installer, true), Part(Jamal, AssignmentRole.Inspector, true)]
    };

    public static TheoryData<InstallationStatus, InstallationStatus> AllPairs() =>
        new(from f in Enum.GetValues<InstallationStatus>() from t in Enum.GetValues<InstallationStatus>() select (f, t));

    [Theory]
    [MemberData(nameof(AllPairs))]
    public void BR_I1_EveryPair_OnlyTheNextStageIsAllowed(InstallationStatus from, InstallationStatus to)
    {
        // Every condition met: what's left is the order of the stages (no skipping, no going back, final is final)
        var installation = Ready(from);
        var index = Array.IndexOf(Forward, from);
        var allowed = index >= 0 && index + 1 < Forward.Length && Forward[index + 1] == to;

        var refusal = installation.TryMoveTo(to, Admin, Now);

        if (allowed)
        {
            refusal.Should().BeNull();
            installation.Status.Should().Be(to);
            installation.History.Should().ContainSingle().Which.Should().BeEquivalentTo(
                new { FromStatus = (InstallationStatus?)from, ToStatus = to, ChangedAt = Now, ChangedByUserId = (int?)Admin });
        }
        else
        {
            refusal!.Code.Should().Be("installation.transition_not_allowed");
            installation.Status.Should().Be(from);
            installation.History.Should().BeEmpty();
        }
    }

    [Fact]
    public void BR_I1_EveryPair_Has64Cases()
    {
        AllPairs().Count.Should().Be(64);
    }

    [Theory]
    [InlineData(InstallationStatus.Survey, "Can't move to Design yet. Assign a surveyor, and have them complete the survey.")]
    [InlineData(InstallationStatus.Design, "Can't move to Permits yet. Record the system size and the panel count first.")]
    [InlineData(InstallationStatus.Permits, "Can't move to Installation yet. Assign at least one installer first.")]
    [InlineData(InstallationStatus.Installation, "Can't move to Inspection yet. Assign at least one installer first.")]
    [InlineData(InstallationStatus.Inspection, "Can't move to Active yet. Assign an inspector, and have them complete the inspection.")]
    public void BR_I1_ConditionNotMet_IsRefusedWithWhatItWaitsFor(InstallationStatus at, string message)
    {
        var installation = new Installation { Status = at, TimeZone = "America/Chicago" };

        var refusal = installation.TryMoveTo(installation.NextStage!.Value, Admin, Now);

        refusal!.Message.Should().Be(message);
        installation.Status.Should().Be(at);
    }

    [Fact]
    public void BR_I1_AssignedButNotCompleted_NamesWhoItWaitsFor()
    {
        var survey = new Installation { Status = InstallationStatus.Survey, Assignments = [Part(Jamal, AssignmentRole.Surveyor, false)] };
        var installing = new Installation
        {
            Status = InstallationStatus.Installation,
            Assignments = [Part(Lena, AssignmentRole.Installer, true), Part(Jamal, AssignmentRole.Installer, false)]
        };

        survey.WaitingFor(InstallationStatus.Design).Should().Be("Jamal Ortiz hasn't completed the survey yet.");
        installing.WaitingFor(InstallationStatus.Inspection).Should().Be("Every installer must complete their part: Jamal Ortiz hasn't yet.");
    }

    [Fact]
    public void BR_I1_SwitchingOn_RecordsWhen()
    {
        var installation = Ready(InstallationStatus.Inspection);

        installation.TryMoveTo(InstallationStatus.Active, Admin, Now);

        installation.ActivatedAt.Should().Be(Now);
        installation.StatusChangedAt.Should().Be(Now);
    }

    [Theory]
    [InlineData(InstallationStatus.Survey, true)]
    [InlineData(InstallationStatus.Design, true)]
    [InlineData(InstallationStatus.Permits, true)]
    [InlineData(InstallationStatus.Installation, true)]
    [InlineData(InstallationStatus.Inspection, true)]
    [InlineData(InstallationStatus.Active, false)]
    [InlineData(InstallationStatus.Cancelled, false)]
    [InlineData(InstallationStatus.Deactivated, false)]
    public void BR_I2_Cancel_OnlyBeforeActive_TheReasonInItsHistoryRow(InstallationStatus at, bool allowed)
    {
        var installation = Ready(at);

        var refusal = installation.Cancel("  The homeowner is selling the house.  ", Admin, Now);

        if (allowed)
        {
            refusal.Should().BeNull();
            installation.Status.Should().Be(InstallationStatus.Cancelled);
            installation.History.Single().Note.Should().Be("The homeowner is selling the house.");
        }
        else
        {
            refusal!.Code.Should().Be("installation.transition_not_allowed");
            installation.Status.Should().Be(at);
        }
    }

    [Theory]
    [InlineData(InstallationStatus.Active, true)]
    [InlineData(InstallationStatus.Inspection, false)]
    [InlineData(InstallationStatus.Cancelled, false)]
    [InlineData(InstallationStatus.Deactivated, false)]
    public void BR_I2_Deactivate_OnlyFromActive(InstallationStatus at, bool allowed)
    {
        var installation = Ready(at);

        var refusal = installation.Deactivate(null, Admin, Now);

        (refusal is null).Should().Be(allowed);
        installation.Status.Should().Be(allowed ? InstallationStatus.Deactivated : at);
    }

    [Theory]
    [InlineData(InstallationStatus.Active, true)]
    [InlineData(InstallationStatus.Inspection, false)]
    [InlineData(InstallationStatus.Cancelled, false)]
    [InlineData(InstallationStatus.Deactivated, false)]
    public void BR_T2_DeviceKey_OnlyWhileActive(InstallationStatus at, bool allowed)
    {
        var installation = Ready(at);

        var refusal = installation.SetDeviceKey("ost46n6gr6ny", [1, 2, 3], Now);

        if (allowed)
        {
            refusal.Should().BeNull();
            (installation.DeviceKeyPrefix, installation.DeviceKeyHash, installation.DeviceKeyCreatedAt)
                .Should().BeEquivalentTo(("ost46n6gr6ny", new byte[] { 1, 2, 3 }, (DateTime?)Now));
        }
        else
        {
            refusal!.Code.Should().Be("installation.not_active");
            installation.DeviceKeyPrefix.Should().BeNull();
        }
    }

    [Fact]
    public void BR_T2_Deactivating_DeletesTheDeviceKey()
    {
        var installation = Ready(InstallationStatus.Active);
        installation.SetDeviceKey("ost46n6gr6ny", [1, 2, 3], Now);

        installation.Deactivate("Removed with the roof.", Admin, Now);

        (installation.DeviceKeyPrefix, installation.DeviceKeyHash, installation.DeviceKeyCreatedAt).Should().Be((null, null, null));
    }

    [Theory]
    [InlineData(InstallationStatus.Permits, false)]
    [InlineData(InstallationStatus.Active, true)]
    [InlineData(InstallationStatus.Cancelled, true)]
    public void BR_P4_DesignAndTimeZone_FixedOnceActiveOrClosed(InstallationStatus at, bool isFixed)
    {
        var installation = Ready(at);

        var design = installation.RecordDesign(10.5m, 26, " Fronius Primo ");
        var zone = installation.SetTimeZone("America/Denver");

        if (isFixed)
        {
            design!.Code.Should().Be("installation.fixed");
            zone!.Code.Should().Be("installation.fixed");
            (installation.SystemSizeKw, installation.TimeZone).Should().Be((8.4m, "America/Chicago"));
        }
        else
        {
            (design, zone).Should().Be((null, null));
            (installation.SystemSizeKw, installation.PanelCount, installation.InverterType, installation.TimeZone).Should()
                .Be((10.5m, 26, "Fronius Primo", "America/Denver"));
        }
    }

    [Fact]
    public void SetTimeZone_NotOneOfTheUsZones_Throws()
    {
        var act = () => Ready(InstallationStatus.Design).SetTimeZone("Europe/Paris");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void FR_I5_Assign_AnActiveTechnician_OncePerRole()
    {
        var installation = new Installation { Status = InstallationStatus.Survey };

        installation.Assign(Jamal, AssignmentRole.Surveyor, Admin, Now).Should().BeNull();
        installation.Assign(Jamal, AssignmentRole.Installer, Admin, Now).Should().BeNull();
        var again = installation.Assign(Jamal, AssignmentRole.Surveyor, Admin, Now);

        again!.Code.Should().Be("assignment.duplicate");
        installation.Assignments.Select(a => (a.TechnicianId, a.Role, a.AssignedByUserId, a.AssignedAt)).Should().Equal(
            (7, AssignmentRole.Surveyor, Admin, Now), (7, AssignmentRole.Installer, Admin, Now));
    }

    [Fact]
    public void FR_I5_Assign_AnAdminOrADeactivatedTechnician_IsRefused()
    {
        var installation = new Installation { Status = InstallationStatus.Survey };
        var admin = new User { Id = 1, Name = "Rosa Castillo", Role = UserRole.Admin, IsActive = true };
        var gone = new User { Id = 9, Name = "Marco Díaz", Role = UserRole.Technician, IsActive = false };

        installation.Assign(admin, AssignmentRole.Surveyor, Admin, Now)!.Code.Should().Be("assignment.not_a_technician");
        installation.Assign(gone, AssignmentRole.Surveyor, Admin, Now)!.Code.Should().Be("assignment.not_a_technician");
        installation.Assignments.Should().BeEmpty();
    }

    [Theory]
    [InlineData(AssignmentRole.Surveyor, InstallationStatus.Survey, true)]
    [InlineData(AssignmentRole.Surveyor, InstallationStatus.Design, false)]
    [InlineData(AssignmentRole.Installer, InstallationStatus.Permits, false)]
    [InlineData(AssignmentRole.Installer, InstallationStatus.Installation, true)]
    [InlineData(AssignmentRole.Inspector, InstallationStatus.Inspection, true)]
    [InlineData(AssignmentRole.Inspector, InstallationStatus.Active, false)]
    public void FR_I6_Complete_OnlyAtItsStage_RecordingWhoAndWhen(AssignmentRole role, InstallationStatus at, bool allowed)
    {
        var part = Part(Jamal, role, false);
        var installation = new Installation { Status = at, Assignments = [part] };

        var refusal = installation.Complete(part, Admin, Now);

        if (allowed)
        {
            refusal.Should().BeNull();
            (part.CompletedAt, part.CompletedByUserId).Should().Be((Now, Admin));
        }
        else
        {
            refusal!.Code.Should().Be("assignment.not_at_its_stage");
            part.CompletedAt.Should().BeNull();
        }
    }

    [Fact]
    public void FR_I6_Complete_Twice_IsRefused()
    {
        var part = Part(Jamal, AssignmentRole.Surveyor, true);
        var installation = new Installation { Status = InstallationStatus.Survey, Assignments = [part] };

        installation.Complete(part, Admin, Now)!.Code.Should().Be("assignment.completed");
    }

    [Fact]
    public void Unassign_ANotCompletedPart_Goes_ACompletedOneStays()
    {
        var open = Part(Lena, AssignmentRole.Installer, false);
        var done = Part(Jamal, AssignmentRole.Surveyor, true);
        var installation = new Installation { Status = InstallationStatus.Permits, Assignments = [open, done] };

        installation.Unassign(open).Should().BeNull();
        installation.Unassign(done)!.Code.Should().Be("assignment.completed");
        installation.Assignments.Should().Equal(done);
    }

    [Fact]
    public void ForConvertedLead_StartsAtSurvey_WithItsCreationInTheHistory()
    {
        var lead = new Lead { Id = 130, AddressId = 12 };

        var installation = Installation.ForConvertedLead(lead, 5, "America/Chicago", Admin, Now);

        (installation.Status, installation.LeadId, installation.AddressId, installation.CustomerId, installation.StatusChangedAt)
            .Should().Be((InstallationStatus.Survey, 130, 12, 5, Now));
        installation.History.Should().ContainSingle().Which.Should().BeEquivalentTo(
            new { FromStatus = (InstallationStatus?)null, ToStatus = InstallationStatus.Survey, ChangedByUserId = (int?)Admin });
    }

    [Theory]
    [InlineData(new InstallationStatus[0], CustomerStatus.Prospect)]
    [InlineData(new[] { InstallationStatus.Survey }, CustomerStatus.Prospect)]
    [InlineData(new[] { InstallationStatus.Cancelled, InstallationStatus.Design }, CustomerStatus.Prospect)]
    [InlineData(new[] { InstallationStatus.Active, InstallationStatus.Survey }, CustomerStatus.Active)]
    [InlineData(new[] { InstallationStatus.Active, InstallationStatus.Cancelled }, CustomerStatus.Active)]
    [InlineData(new[] { InstallationStatus.Cancelled }, CustomerStatus.Inactive)]
    [InlineData(new[] { InstallationStatus.Cancelled, InstallationStatus.Deactivated }, CustomerStatus.Inactive)]
    public void BR_C1_CustomerStatus_FollowsTheInstallations(InstallationStatus[] installations, CustomerStatus expected)
    {
        Customer.StatusFor(installations).Should().Be(expected);
    }
}
