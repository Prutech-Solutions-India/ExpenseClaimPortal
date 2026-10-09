using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

using FluentAssertions;

using Xunit;
using Xunit.Abstractions;

namespace PilotService.Tests.Integration;

/// <summary>
/// AC-5, both ways round. A probe that reports healthy whatever the database is doing is worse
/// than no probe at all: it keeps a broken instance in the load balancer. So the negative case
/// is tested by pointing a second host at a database that cannot be reached - no bypass flag,
/// no stubbed check.
/// </summary>
[Collection(PostgresCollection.Name)]
[Trait("Category", "Integration")]
public sealed class HealthTests
{
    private readonly PostgresFixture _postgres;
    private readonly ITestOutputHelper _output;

    public HealthTests(PostgresFixture postgres, ITestOutputHelper output)
    {
        _postgres = postgres;
        _output = output;
    }

    [Fact]
    public async Task ReportsHealthyIncludingDatabaseReachability()
    {
        if (!_postgres.EnsureAvailable(_output))
        {
            return;
        }

        await using var factory = _postgres.CreateFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/health");

        // Mapped outside the /api group, so it answers a platform probe that carries no identity.
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadAsStringAsync();
        var health = TestJson.Deserialize<HealthPayload>(body);

        health.Should().NotBeNull();
        health!.Status.Should().Be("Healthy");
        health.Checks.Should().NotBeEmpty();
        health.Checks.Should().Contain(
            check => check.Name.Contains("data", StringComparison.OrdinalIgnoreCase)
                     || check.Name.Contains("db", StringComparison.OrdinalIgnoreCase),
            "the report has to say whether the database was reachable");
        health.Checks.Should().OnlyContain(check => check.Status == "Healthy");
    }

    [Fact]
    public async Task ReportsUnhealthyWhenTheDatabaseCannotBeReached()
    {
        // Needs no fixture database: the point is a host whose database is absent, which is
        // exactly the situation the probe exists for.
        await using var factory = _postgres.CreateFactory(PostgresFixture.UnreachableConnectionString);
        var client = factory.CreateClient();

        var response = await client.GetAsync("/health");

        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);

        var body = await response.Content.ReadAsStringAsync();
        var health = TestJson.Deserialize<HealthPayload>(body);

        health.Should().NotBeNull();
        health!.Status.Should().Be("Unhealthy");
        health.Checks.Should().NotBeEmpty();
        health.Checks.Should().Contain(check => check.Status == "Unhealthy");
    }

    [Fact]
    public async Task StaysOutsideTheApiGroupSoItNeedsNoActingIdentity()
    {
        await using var factory = _postgres.CreateFactory(
            _postgres.IsAvailable ? null : PostgresFixture.UnreachableConnectionString);
        var client = factory.CreateClient();

        var response = await client.GetAsync("/health");

        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
        response.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
        response.StatusCode.Should().NotBe(HttpStatusCode.NotFound);

        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("\"status\"");
        body.Should().NotContain("<div id=\"root\"", "the probe is JSON, never the shell");
    }
}
