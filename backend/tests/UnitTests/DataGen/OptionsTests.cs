using FluentAssertions;
using Solaris.DataGen;

namespace UnitTests.DataGen;

// What the dataset generator was asked for (measurements.md §5).
public class OptionsTests
{
    private static (Options? Options, string? Problem) Parse(params string[] args) =>
        Options.Parse(["--connection", "Host=localhost", .. args]);

    [Fact]
    public void WithoutOptions_TheDefaultsAreDatasetM()
    {
        var (options, problem) = Parse();

        problem.Should().BeNull();
        options.Should().BeEquivalentTo(new
        {
            Installations = 1000, Days = 120, Seed = 42, Schema = Schema.Naive, KeysPath = "perf/keys.csv"
        });
    }

    [Fact]
    public void EveryOption_IsRead()
    {
        var (options, _) = Parse("--installations", "5", "--days", "2", "--seed", "7", "--schema", "final", "--keys", "/tmp/k.csv");

        options.Should().BeEquivalentTo(new
        {
            Installations = 5, Days = 2, Seed = 7, Schema = Schema.Final, KeysPath = "/tmp/k.csv"
        });
    }

    [Fact]
    public void TheLayoutIsReadWhateverItsCase()
    {
        Parse("--schema", "FINAL").Options!.Schema.Should().Be(Schema.Final);
        Parse("--schema", "Naive").Options!.Schema.Should().Be(Schema.Naive);
    }

    [Fact]
    public void NoDays_IsTheFleetAlone_ForTheIngestionRunToFill()
    {
        var (options, problem) = Parse("--days", "0");

        problem.Should().BeNull();
        options!.Days.Should().Be(0);
        Parse("--days", "-1").Options.Should().BeNull();
    }

    [Theory]
    [InlineData("--installations", "0")]
    [InlineData("--installations", "-1")]
    [InlineData("--days", "nope")]
    [InlineData("--seed", "1.5")]
    [InlineData("--schema", "sideways")]
    [InlineData("--nonsense", "1")]
    public void SomethingThatIsntAnOption_IsRefusedWithAReason(string name, string value)
    {
        var (options, problem) = Parse(name, value);

        options.Should().BeNull();
        problem.Should().NotBeNullOrWhiteSpace().And.Contain(name.TrimStart('-'));
    }

    [Fact]
    public void AnOptionWithoutAValue_IsRefused()
    {
        Options.Parse(["--connection", "Host=localhost", "--days"]).Problem.Should().NotBeNull();
        Options.Parse(["days", "2"]).Problem.Should().NotBeNull();
    }

    [Fact]
    public void WithoutADatabase_ItSaysSo()
    {
        var wasSet = Environment.GetEnvironmentVariable("ConnectionStrings__Default");
        Environment.SetEnvironmentVariable("ConnectionStrings__Default", null);
        try
        {
            var (options, problem) = Options.Parse(["--installations", "5"]);

            options.Should().BeNull();
            problem.Should().Contain("--connection");
        }
        finally
        {
            Environment.SetEnvironmentVariable("ConnectionStrings__Default", wasSet);
        }
    }
}
