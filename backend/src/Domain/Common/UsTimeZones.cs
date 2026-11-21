namespace Domain.Common;

/// <summary>
/// The US time zones an installation can be in (BR-P4), by today's rules: zones that differ only in their history, such
/// as <c>America/Detroit</c> and <c>America/New_York</c>, count the same local days today, so one name stands for each.
/// A new installation is proposed the zone most of its state lives in; the Admin confirms it, or picks another, until
/// it is Active (several states span two zones: Texas is Central, but El Paso is Mountain).
/// </summary>
public static class UsTimeZones
{
    public const string Eastern = "America/New_York";
    public const string Central = "America/Chicago";
    public const string Mountain = "America/Denver";
    /// <summary>Mountain time without daylight saving.</summary>
    public const string Arizona = "America/Phoenix";
    public const string Pacific = "America/Los_Angeles";
    public const string Alaska = "America/Anchorage";
    /// <summary>Hawaii–Aleutian time with daylight saving: the Aleutian Islands west of 169° 30′ W.</summary>
    public const string Aleutian = "America/Adak";
    /// <summary>Hawaii–Aleutian time without daylight saving.</summary>
    public const string Hawaii = "Pacific/Honolulu";

    /// <summary>Every zone, east to west, with the name the staff app shows.</summary>
    public static readonly IReadOnlyList<(string Id, string Name)> All =
    [
        (Eastern, "Eastern"), (Central, "Central"), (Mountain, "Mountain"), (Arizona, "Arizona"), (Pacific, "Pacific"),
        (Alaska, "Alaska"), (Aleutian, "Aleutian"), (Hawaii, "Hawaii")
    ];

    private static readonly IReadOnlyDictionary<string, string> ByState = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["CT"] = Eastern, ["DE"] = Eastern, ["DC"] = Eastern, ["FL"] = Eastern, ["GA"] = Eastern, ["IN"] = Eastern,
        ["KY"] = Eastern, ["ME"] = Eastern, ["MD"] = Eastern, ["MA"] = Eastern, ["MI"] = Eastern, ["NH"] = Eastern,
        ["NJ"] = Eastern, ["NY"] = Eastern, ["NC"] = Eastern, ["OH"] = Eastern, ["PA"] = Eastern, ["RI"] = Eastern,
        ["SC"] = Eastern, ["VT"] = Eastern, ["VA"] = Eastern, ["WV"] = Eastern,

        ["AL"] = Central, ["AR"] = Central, ["IL"] = Central, ["IA"] = Central, ["KS"] = Central, ["LA"] = Central,
        ["MN"] = Central, ["MS"] = Central, ["MO"] = Central, ["NE"] = Central, ["ND"] = Central, ["OK"] = Central,
        ["SD"] = Central, ["TN"] = Central, ["TX"] = Central, ["WI"] = Central,

        ["CO"] = Mountain, ["ID"] = Mountain, ["MT"] = Mountain, ["NM"] = Mountain, ["UT"] = Mountain, ["WY"] = Mountain,
        ["AZ"] = Arizona,
        ["CA"] = Pacific, ["NV"] = Pacific, ["OR"] = Pacific, ["WA"] = Pacific,
        ["AK"] = Alaska,
        ["HI"] = Hawaii
    };

    /// <summary>The zone proposed for an installation in this state (a code from <see cref="UsStates"/>).</summary>
    public static string ProposedFor(string stateCode) =>
        ProposalFor(stateCode)?.Id
        ?? throw new ArgumentException($"'{stateCode}' isn't the code of a US state or DC.", nameof(stateCode));

    /// <summary>The zone proposed for this state, with its name; null if it isn't a state's code.</summary>
    public static (string Id, string Name)? ProposalFor(string stateCode) =>
        ByState.TryGetValue(stateCode, out var zone) ? All.Single(z => z.Id == zone) : null;
}
