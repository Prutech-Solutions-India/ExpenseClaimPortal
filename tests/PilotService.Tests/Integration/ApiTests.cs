using System.Net;
using System.Net.Http.Json;

using FluentAssertions;

using Microsoft.AspNetCore.Mvc.Testing;

using Xunit;

namespace PilotService.Tests.Integration;

[Trait("Category", "Integration")]
public sealed class ApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ApiTests(WebApplicationFactory<Program> factory) => _factory = factory;

    [Fact]
    public async Task ReportsItsHealthWithTheBuildItIsRunning()
    {
        var response = await _factory.CreateClient().GetAsync("/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync()).Should().Contain("\"status\":\"ok\"");
    }

    [Fact]
    public async Task AcceptsAValidGreeting()
    {
        var response = await _factory.CreateClient()
            .PostAsJsonAsync("/api/greetings", new { name = "Ada" });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<GreetingResponse>();
        body!.Greeting.Should().Be("Hello, Ada.");
    }

    [Fact]
    public async Task RejectsAnInvalidBodyWithTheReasonNotABare400()
    {
        var response = await _factory.CreateClient()
            .PostAsJsonAsync("/api/greetings", new { name = "" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await response.Content.ReadAsStringAsync()).Should().Contain("must not be empty");
    }

    [Fact]
    public async Task AnswersAnUnknownApiRouteWithNotFoundNotThePage()
    {
        var response = await _factory.CreateClient().GetAsync("/api/nothing-here");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
    }

    // The two layers are one deployment, so the joint between them is worth a
    // test of its own: this fails if the bundle was never built into wwwroot,
    // which is the difference between a working product and an API that
    // answers a page nobody can reach.
    [Fact]
    public async Task ServesTheFrontendShellForAClientSideRoute()
    {
        var response = await _factory.CreateClient().GetAsync("/some/client/route");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync()).Should().Contain("<div id=\"root\">");
    }
}
