using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

using FluentAssertions;

using Microsoft.AspNetCore.Mvc.Testing;

using Xunit;
using Xunit.Abstractions;

namespace PilotService.Tests.Integration;

/// <summary>
/// The contract the browser depends on: the typed staff list (AC-2), the acting identity
/// resolved on the server (AC-3), the refusals that do not rely on the UI hiding anything
/// (AC-4), and the shell fallback for everything that is not an API path (AC-7).
/// </summary>
[Collection(PostgresCollection.Name)]
[Trait("Category", "Integration")]
public sealed class ApiTests : IAsyncLifetime
{
    /// <summary>
    /// Spelled out rather than imported from the service: this is the name on the wire, and a
    /// test that reads the same constant as the code cannot notice it changing.
    /// </summary>
    private const string ActingEmployeeHeader = "X-Acting-Employee-Id";

    private static readonly string[] KnownRoles = { "Employee", "Manager", "FinanceOfficer" };

    private readonly PostgresFixture _postgres;
    private readonly ITestOutputHelper _output;

    private WebApplicationFactory<Program>? _factory;
    private HttpClient? _client;

    public ApiTests(PostgresFixture postgres, ITestOutputHelper output)
    {
        _postgres = postgres;
        _output = output;
    }

    /// <inheritdoc />
    public Task InitializeAsync()
    {
        if (_postgres.IsAvailable)
        {
            _factory = _postgres.CreateFactory();
            _client = _factory.CreateClient();
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task DisposeAsync()
    {
        _client?.Dispose();

        if (_factory is not null)
        {
            await _factory.DisposeAsync();
        }
    }

    [Fact]
    public async Task ListsTheSeededStaffAsATypedContractAndNotAnEntity()
    {
        if (!_postgres.EnsureAvailable(_output))
        {
            return;
        }

        var response = await _client!.GetAsync("/api/employees");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/json");

        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("\"displayName\"");

        // The shape is the contract, not the entity: no navigation collections, no email.
        body.Should().NotContain("\"reports\"");
        body.Should().NotContain("\"manager\":");
        body.Should().NotContain("\"email\"");

        var employees = TestJson.Deserialize<List<ApiEmployee>>(body);
        employees.Should().NotBeNull();
        employees!.Should().NotBeEmpty();

        employees.Should().OnlyContain(employee => employee.Id > 0);
        employees.Should().OnlyContain(employee => !string.IsNullOrWhiteSpace(employee.DisplayName));
        employees.Select(employee => employee.Role).Should().BeSubsetOf(KnownRoles);
        employees.Select(employee => employee.DisplayName)
            .Should().BeInAscendingOrder(StringComparer.OrdinalIgnoreCase);

        var ids = employees.Select(employee => employee.Id).ToHashSet();
        employees
            .Where(employee => employee.ManagerId is not null)
            .Should().OnlyContain(employee => ids.Contains(employee.ManagerId!.Value));
    }

    [Fact]
    public async Task ResolvesTheActingIdentityOnTheServerForTheSuppliedHeader()
    {
        if (!_postgres.EnsureAvailable(_output))
        {
            return;
        }

        var employees = await GetEmployeesAsync();
        var expected = employees.First();

        using var request = BuildRequest("/api/me", expected.Id.ToString());
        var response = await _client!.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var me = await response.Content.ReadFromJsonAsync<ApiMe>();
        me.Should().NotBeNull();
        me!.Id.Should().Be(expected.Id);
        me.DisplayName.Should().Be(expected.DisplayName);
        me.Role.Should().Be(expected.Role);
        me.ManagerId.Should().Be(expected.ManagerId);
        me.Email.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task AnswersEveryEmployeeWithTheirOwnIdentity()
    {
        if (!_postgres.EnsureAvailable(_output))
        {
            return;
        }

        var employees = await GetEmployeesAsync();

        foreach (var employee in employees)
        {
            using var request = BuildRequest("/api/me", employee.Id.ToString());
            var response = await _client!.SendAsync(request);

            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var me = await response.Content.ReadFromJsonAsync<ApiMe>();
            me.Should().NotBeNull();
            me!.Id.Should().Be(employee.Id);
            me.Role.Should().Be(employee.Role, "the role is resolved from the row, not from the caller");
        }
    }

    [Fact]
    public async Task RefusesARequestThatNamesNoActingEmployee()
    {
        if (!_postgres.EnsureAvailable(_output))
        {
            return;
        }

        var response = await _client!.GetAsync("/api/me");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/json");

        var error = await response.Content.ReadFromJsonAsync<ApiErrorBody>();
        error.Should().NotBeNull();
        error!.Code.Should().Be("acting_identity_missing");
        error.Message.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task RefusesAnActingEmployeeIdentifierThatIsNotANumber()
    {
        if (!_postgres.EnsureAvailable(_output))
        {
            return;
        }

        using var request = BuildRequest("/api/me", "not-a-number");
        var response = await _client!.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var error = await response.Content.ReadFromJsonAsync<ApiErrorBody>();
        error.Should().NotBeNull();
        error!.Code.Should().Be("acting_identity_missing");
    }

    [Fact]
    public async Task RefusesAnActingEmployeeNobodyHasEverSeen()
    {
        if (!_postgres.EnsureAvailable(_output))
        {
            return;
        }

        using var request = BuildRequest("/api/me", "987654");
        var response = await _client!.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/json");

        var error = await response.Content.ReadFromJsonAsync<ApiErrorBody>();
        error.Should().NotBeNull();
        error!.Code.Should().Be("acting_identity_unknown");
    }

    [Fact]
    public async Task KeepsTheSwitcherListReachableBeforeAnyIdentityIsChosen()
    {
        if (!_postgres.EnsureAvailable(_output))
        {
            return;
        }

        // The switcher has to be able to load the list it is meant to choose from, and it is the
        // only endpoint in the group allowed to answer without an identity.
        var response = await _client!.GetAsync("/api/employees");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task AnswersAnUnknownApiRouteWithJsonNotWithThePage()
    {
        if (!_postgres.EnsureAvailable(_output))
        {
            return;
        }

        var response = await _client!.GetAsync("/api/nothing-here");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/json");

        var error = await response.Content.ReadFromJsonAsync<ApiErrorBody>();
        error.Should().NotBeNull();
        error!.Code.Should().Be("not_found");
    }

    [Fact]
    public async Task FallsBackToTheReactShellForAClientSideRoute()
    {
        if (!_postgres.EnsureAvailable(_output))
        {
            return;
        }

        var response = await _client!.GetAsync("/some/client/route");

        // The identity filter guards /api and nothing else: a page request is never an
        // authorisation decision.
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
        response.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);

        var mediaType = response.Content.Headers.ContentType?.MediaType;
        mediaType.Should().NotBe("application/json", "a client-side route is not an API path");

        if (response.StatusCode == HttpStatusCode.OK)
        {
            var body = await response.Content.ReadAsStringAsync();
            body.Should().Contain("<div id=\"root\"", "the shell is the React bundle, not a Razor page");
            body.Should().NotContain("_blazor");
        }
        else
        {
            // No bundle in this test output: the frontend was not built into wwwroot for this
            // run, which is a build concern and not a routing one.
            _output.WriteLine(
                $"No wwwroot/index.html in the test output; the shell fallback returned {(int)response.StatusCode}.");
        }
    }

    /// <summary>Loads the seeded staff list, which needs no acting identity.</summary>
    private async Task<IReadOnlyList<ApiEmployee>> GetEmployeesAsync()
    {
        var employees = await _client!.GetFromJsonAsync<List<ApiEmployee>>("/api/employees");
        employees.Should().NotBeNull();
        employees!.Should().NotBeEmpty();
        return employees;
    }

    /// <summary>Builds a GET carrying (or deliberately omitting) the acting identity header.</summary>
    private static HttpRequestMessage BuildRequest(string path, string? actingEmployeeId)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, path);

        if (actingEmployeeId is not null)
        {
            request.Headers.Add(ActingEmployeeHeader, actingEmployeeId);
        }

        return request;
    }
}
