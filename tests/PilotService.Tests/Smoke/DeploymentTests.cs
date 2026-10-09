using System.Net;

using FluentAssertions;

using Xunit;

namespace PilotService.Tests.Smoke;

[Trait("Category", "Smoke")]
public sealed class DeploymentTests
{
    private static string BaseUrl =>
        (Environment.GetEnvironmentVariable("BASE_URL") ?? string.Empty).TrimEnd('/');

    // Skipped rather than passed when there is no deployment to talk to. A
    // smoke suite that goes green against nothing is the failure this whole
    // pipeline exists to prevent.
    [SkippableFact]
    public async Task AnswersItsHealthCheck()
    {
        Skip.If(BaseUrl.Length == 0, "BASE_URL is not set, so there is no deployment to check.");

        using var client = new HttpClient();
        var response = await client.GetAsync($"{BaseUrl}/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync()).Should().Contain("\"status\":\"ok\"");
    }
}
