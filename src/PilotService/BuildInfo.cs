namespace PilotService;

/// <summary>
/// What this build is, so a deployment can be traced to the commit that made it.
/// </summary>
/// <remarks>
/// Missing values are reported as "unknown" rather than guessed: a smoke check
/// that invents a commit is worse than one that admits it has none.
/// </remarks>
public sealed record BuildInfo(string Version, string Commit)
{
    public static BuildInfo FromEnvironment() => new(
        Environment.GetEnvironmentVariable("APP_VERSION") ?? "0.0.0-dev",
        Environment.GetEnvironmentVariable("COMMIT_SHA") ?? "unknown");
}
