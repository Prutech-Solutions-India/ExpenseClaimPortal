using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

using FluentAssertions;

using Microsoft.EntityFrameworkCore;

using Xunit;
using Xunit.Abstractions;

namespace PilotService.Tests.Integration;

/// <summary>
/// AC-1: a clean database and a connection string are the whole setup. Starting the host must
/// apply the migrations and the seed by itself, and starting it again must not double the rows -
/// a migration nobody runs is the same as no table at all, and a seed that is not idempotent
/// turns every restart into a data incident.
/// </summary>
[Collection(PostgresCollection.Name)]
[Trait("Category", "Integration")]
public sealed class DatabaseStartupTests
{
    private const int SeededRoleCount = 3;
    private const int SeededEmployeeCount = 5;

    private readonly PostgresFixture _postgres;
    private readonly ITestOutputHelper _output;

    public DatabaseStartupTests(PostgresFixture postgres, ITestOutputHelper output)
    {
        _postgres = postgres;
        _output = output;
    }

    [Fact]
    public async Task AppliesMigrationsAndSeedDataToACleanDatabaseWithNoManualStep()
    {
        if (!_postgres.EnsureAvailable(_output))
        {
            return;
        }

        (await _postgres.ResetDatabaseAsync()).Should().BeTrue("the test needs a clean database");

        await using var factory = _postgres.CreateFactory();
        var client = factory.CreateClient();

        // Reaching the API at all proves the schema exists: nothing else created it.
        var response = await client.GetAsync("/api/employees");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var employees = await response.Content.ReadFromJsonAsync<List<ApiEmployee>>();
        employees.Should().NotBeNull();
        employees!.Should().HaveCount(SeededEmployeeCount);

        await using var context = _postgres.CreateDbContext();

        var applied = await context.Database.GetAppliedMigrationsAsync();
        applied.Should().NotBeEmpty("the host applies the explicit migrations at startup");

        (await context.Roles.CountAsync()).Should().Be(SeededRoleCount);
        (await context.Employees.CountAsync()).Should().Be(SeededEmployeeCount);

        employees.Select(employee => employee.Role).Distinct()
            .Should().BeEquivalentTo(new[] { "Employee", "Manager", "FinanceOfficer" });

        // Reporting lines are part of the seed, not an afterthought: somebody reports to somebody.
        employees.Should().Contain(employee => employee.ManagerId != null);
        employees.Should().Contain(employee => employee.ManagerId == null);
    }

    [Fact]
    public async Task LeavesTheSeededRowsUntouchedWhenTheApplicationRestarts()
    {
        if (!_postgres.EnsureAvailable(_output))
        {
            return;
        }

        (await _postgres.ResetDatabaseAsync()).Should().BeTrue("the test needs a clean database");

        IReadOnlyList<ApiEmployee> firstRun;
        await using (var factory = _postgres.CreateFactory())
        {
            var client = factory.CreateClient();
            var employees = await client.GetFromJsonAsync<List<ApiEmployee>>("/api/employees");
            employees.Should().NotBeNull();
            firstRun = employees!;
        }

        firstRun.Should().HaveCount(SeededEmployeeCount);

        IReadOnlyList<ApiEmployee> secondRun;
        await using (var factory = _postgres.CreateFactory())
        {
            var client = factory.CreateClient();
            var employees = await client.GetFromJsonAsync<List<ApiEmployee>>("/api/employees");
            employees.Should().NotBeNull();
            secondRun = employees!;
        }

        secondRun.Should().BeEquivalentTo(firstRun, "a restart must not seed a second copy");

        await using var context = _postgres.CreateDbContext();
        (await context.Roles.CountAsync()).Should().Be(SeededRoleCount);
        (await context.Employees.CountAsync()).Should().Be(SeededEmployeeCount);

        secondRun.Select(employee => employee.Id).Should().OnlyHaveUniqueItems();
        secondRun.Select(employee => employee.DisplayName).Should().OnlyHaveUniqueItems();
    }
}
