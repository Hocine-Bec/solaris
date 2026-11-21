using Domain.Entities;
using Domain.Enums;
using FluentAssertions;

namespace UnitTests.Domain;

public class LeadTests
{
    private static readonly DateTime Earlier = new(2026, 11, 19, 16, 5, 0, DateTimeKind.Utc);
    private static readonly DateTime Now = new(2026, 11, 20, 9, 30, 0, DateTimeKind.Utc);

    // BR-L2, every pair: from each status, to each status (itself included)
    private static readonly HashSet<(LeadStatus From, LeadStatus To)> Allowed =
    [
        (LeadStatus.New, LeadStatus.Contacted), (LeadStatus.New, LeadStatus.Disqualified), (LeadStatus.New, LeadStatus.Lost),
        (LeadStatus.Contacted, LeadStatus.Converted), (LeadStatus.Contacted, LeadStatus.Disqualified),
        (LeadStatus.Contacted, LeadStatus.Lost)
    ];

    public static TheoryData<LeadStatus, LeadStatus> AllowedPairs() =>
        new(Allowed.Select(p => (p.From, p.To)));

    public static TheoryData<LeadStatus, LeadStatus> RefusedPairs() =>
        new(from f in Enum.GetValues<LeadStatus>()
            from t in Enum.GetValues<LeadStatus>()
            where !Allowed.Contains((f, t))
            select (f, t));

    private static Lead At(LeadStatus status) =>
        new() { Status = status, CreatedAt = Earlier, ContactedAt = status == LeadStatus.New ? null : Earlier };

    [Fact]
    public void Lead_DefaultStatus_ShouldBeNew()
    {
        new Lead().Status.Should().Be(LeadStatus.New);
    }

    [Fact]
    public void Lead_Times_AreNullByDefault()
    {
        var lead = new Lead();

        lead.ContactedAt.Should().BeNull();
        lead.ClosedAt.Should().BeNull();
        lead.CustomerId.Should().BeNull();
    }

    [Fact]
    public void BR_L2_Pairs_CoverEveryStatusToEveryStatus()
    {
        // 5 statuses: 25 pairs, 6 allowed and 19 refused
        var count = Enum.GetValues<LeadStatus>().Length;
        (AllowedPairs().Count + RefusedPairs().Count).Should().Be(count * count);
        AllowedPairs().Count.Should().Be(6);
    }

    [Theory]
    [MemberData(nameof(AllowedPairs))]
    public void BR_L2_AllowedMove_ChangesTheStatus(LeadStatus from, LeadStatus to)
    {
        var lead = At(from);

        lead.TryMoveTo(to, Now).Should().BeTrue();

        lead.Status.Should().Be(to);
    }

    [Theory]
    [MemberData(nameof(RefusedPairs))]
    public void BR_L2_RefusedMove_ChangesNothing(LeadStatus from, LeadStatus to)
    {
        var lead = At(from);
        var closedAt = lead.ClosedAt;

        lead.TryMoveTo(to, Now).Should().BeFalse();

        lead.Status.Should().Be(from);
        lead.ContactedAt.Should().Be(from == LeadStatus.New ? null : Earlier);
        lead.ClosedAt.Should().Be(closedAt);
    }

    [Fact]
    public void BR_L2_Contacted_RecordsWhenItWasContacted()
    {
        var lead = At(LeadStatus.New);

        lead.TryMoveTo(LeadStatus.Contacted, Now);

        lead.ContactedAt.Should().Be(Now);
        lead.ClosedAt.Should().BeNull();
    }

    [Theory]
    [InlineData(LeadStatus.New, LeadStatus.Disqualified)]
    [InlineData(LeadStatus.New, LeadStatus.Lost)]
    [InlineData(LeadStatus.Contacted, LeadStatus.Converted)]
    [InlineData(LeadStatus.Contacted, LeadStatus.Disqualified)]
    [InlineData(LeadStatus.Contacted, LeadStatus.Lost)]
    public void BR_L2_Closing_RecordsWhenItClosed_AndKeepsTheContactTime(LeadStatus from, LeadStatus to)
    {
        var lead = At(from);

        lead.TryMoveTo(to, Now);

        lead.ClosedAt.Should().Be(Now);
        lead.ContactedAt.Should().Be(from == LeadStatus.Contacted ? Earlier : null);
    }

    [Theory]
    [InlineData(LeadStatus.New, new[] { LeadStatus.Contacted, LeadStatus.Disqualified, LeadStatus.Lost })]
    [InlineData(LeadStatus.Contacted, new[] { LeadStatus.Converted, LeadStatus.Disqualified, LeadStatus.Lost })]
    [InlineData(LeadStatus.Converted, new LeadStatus[0])]
    [InlineData(LeadStatus.Disqualified, new LeadStatus[0])]
    [InlineData(LeadStatus.Lost, new LeadStatus[0])]
    public void BR_L2_NextStatuses_AreTheAllowedMoves(LeadStatus from, LeadStatus[] next)
    {
        At(from).NextStatuses.Should().Equal(next);
    }

    [Theory]
    [InlineData(LeadStatus.New, true)]
    [InlineData(LeadStatus.Contacted, true)]
    [InlineData(LeadStatus.Converted, false)]
    [InlineData(LeadStatus.Disqualified, false)]
    [InlineData(LeadStatus.Lost, false)]
    public void BR_L1_IsOpen_WhileNewOrContacted(LeadStatus status, bool open)
    {
        At(status).IsOpen.Should().Be(open);
    }

    [Fact]
    public void FullName_IsTheFirstThenTheLastName()
    {
        new Lead { FirstName = "Dana", LastName = "Whitfield" }.FullName.Should().Be("Dana Whitfield");
    }
}
