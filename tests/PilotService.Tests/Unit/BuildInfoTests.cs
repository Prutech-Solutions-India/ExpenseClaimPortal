using FluentAssertions;

using Xunit;

namespace PilotService.Tests.Unit;

[Trait("Category", "Unit")]
public sealed class BuildInfoTests
{
    [Fact]
    public void AdmitsWhenTheCommitWasNotInjectedAtBuildTime()
    {
        // A deployed build with no commit is a traceability gap, so the smoke
        // check has to be able to see it rather than read a plausible default.
        Environment.SetEnvironmentVariable("COMMIT_SHA", null);

        BuildInfo.FromEnvironment().Commit.Should().Be("unknown");
    }

    [Fact]
    public void ReportsTheCommitItWasBuiltFrom()
    {
        Environment.SetEnvironmentVariable("COMMIT_SHA", "abc123");
        try
        {
            BuildInfo.FromEnvironment().Commit.Should().Be("abc123");
        }
        finally
        {
            Environment.SetEnvironmentVariable("COMMIT_SHA", null);
        }
    }
}
