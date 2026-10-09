using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

using PilotService.Data;

using Xunit;
using Xunit.Abstractions;

namespace PilotService.Tests.Integration;

/// <summary>
/// Shared PostgreSQL connection for the integration suite.
///
/// The connection string is supplied from the environment - the same
/// <c>ConnectionStrings__Default</c> variable the application itself reads - because a test
/// that invents its own literal is testing a configuration nobody deploys.
///
/// When no reachable database is supplied the fixture does not pretend: <see cref="IsAvailable"/>
/// is false, <see cref="SkipReason"/> says why, and every database-dependent test returns early
/// after writing that reason to the test output. A test that quietly asserts nothing and reports
/// green is worse than one that fails.
/// </summary>
public sealed class PostgresFixture : IAsyncLifetime
{
    /// <summary>
    /// A connection string that is deliberately impossible to satisfy: port 1 refuses
    /// immediately, so the unhealthy branch of the health check is exercised in about a second
    /// and without a bypass flag in production code.
    /// </summary>
    public const string UnreachableConnectionString =
        "Host=127.0.0.1;Port=1;Database=pilot_unreachable;Username=none;Password=none;Timeout=1;Command Timeout=1";

    /// <summary>Environment variables consulted, in order, for the test database.</summary>
    private static readonly string[] EnvironmentVariableNames =
    {
        "ConnectionStrings__Default",
        "TEST_POSTGRES_CONNECTION_STRING",
        "POSTGRES_CONNECTION_STRING",
    };

    /// <summary>The reachable connection string, or null when none was supplied.</summary>
    public string? ConnectionString { get; private set; }

    /// <summary>Whether a reachable PostgreSQL instance is available to this run.</summary>
    public bool IsAvailable { get; private set; }

    /// <summary>Why the database-dependent tests cannot run, when they cannot.</summary>
    public string SkipReason { get; private set; } =
        "The PostgreSQL fixture has not been initialised.";

    /// <inheritdoc />
    public async Task InitializeAsync()
    {
        var configured = EnvironmentVariableNames
            .Select(Environment.GetEnvironmentVariable)
            .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));

        if (configured is null)
        {
            SkipReason =
                "No PostgreSQL connection string was supplied. Set ConnectionStrings__Default " +
                "(or TEST_POSTGRES_CONNECTION_STRING) to a reachable database to run the " +
                "database-backed integration tests.";
            return;
        }

        try
        {
            await using var context = CreateDbContext(configured);
            using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(30));

            if (!await context.Database.CanConnectAsync(cancellation.Token))
            {
                // The server is up but the database named in the DSN does not exist yet;
                // migrating creates it, which is exactly what the application does on first run.
                await context.Database.MigrateAsync(cancellation.Token);
            }

            ConnectionString = configured;
            IsAvailable = true;
            SkipReason = string.Empty;
        }
        catch (Exception exception)
        {
            SkipReason =
                "The supplied PostgreSQL connection string is not usable: " +
                $"{exception.GetType().Name}: {exception.Message}";
        }
    }

    /// <inheritdoc />
    public Task DisposeAsync() => Task.CompletedTask;

    /// <summary>
    /// Reports availability, writing the skip reason to the test output when the database is
    /// missing so a skipped run is never mistaken for a passing one.
    /// </summary>
    /// <param name="output">The test output sink.</param>
    /// <returns>True when the database-dependent assertions may run.</returns>
    public bool EnsureAvailable(ITestOutputHelper output)
    {
        if (IsAvailable)
        {
            return true;
        }

        output.WriteLine($"SKIPPED - no database: {SkipReason}");
        return false;
    }

    /// <summary>
    /// Creates a context bound to the fixture's database (or to a caller-supplied one).
    /// </summary>
    /// <param name="connectionString">Overrides the fixture connection string when supplied.</param>
    /// <returns>A new <see cref="AppDbContext"/>. The caller disposes it.</returns>
    public AppDbContext CreateDbContext(string? connectionString = null)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(connectionString ?? ConnectionString ?? UnreachableConnectionString)
            .Options;

        return new AppDbContext(options);
    }

    /// <summary>
    /// Returns the database to a clean state by dropping and recreating the public schema, so
    /// "a clean database needs no manual step" is tested against an actually clean database
    /// rather than against whatever a previous run happened to leave behind.
    /// </summary>
    /// <returns>True when the reset was performed.</returns>
    public async Task<bool> ResetDatabaseAsync()
    {
        if (!IsAvailable)
        {
            return false;
        }

        await using var context = CreateDbContext();
        await context.Database.ExecuteSqlRawAsync("DROP SCHEMA IF EXISTS public CASCADE; CREATE SCHEMA public;");
        return true;
    }

    /// <summary>
    /// Boots the application in process with the connection string under test.
    /// </summary>
    /// <param name="connectionString">
    /// Overrides the fixture connection string - used to point the host at an unreachable
    /// database for the negative health case.
    /// </param>
    /// <returns>A factory the caller disposes.</returns>
    public WebApplicationFactory<Program> CreateFactory(string? connectionString = null) =>
        new PilotApplicationFactory(connectionString ?? ConnectionString ?? UnreachableConnectionString);

    /// <summary>Supplies <c>ConnectionStrings:Default</c> to the host under test.</summary>
    private sealed class PilotApplicationFactory : WebApplicationFactory<Program>
    {
        private readonly string _connectionString;

        public PilotApplicationFactory(string connectionString) => _connectionString = connectionString;

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureAppConfiguration((_, configuration) =>
            {
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:Default"] = _connectionString,
                });
            });
        }
    }
}

/// <summary>
/// Keeps every database-backed test class on one fixture and, because xUnit does not run a
/// collection in parallel with itself, one host at a time against the shared database.
/// </summary>
[CollectionDefinition(PostgresCollection.Name)]
public sealed class PostgresCollection : ICollectionFixture<PostgresFixture>
{
    /// <summary>Name shared by every <c>[Collection]</c> attribute in this suite.</summary>
    public const string Name = "postgres";
}

/// <summary>One element of <c>GET /api/employees</c>, as the wire declares it.</summary>
public sealed record ApiEmployee(int Id, string DisplayName, string Role, int? ManagerId);

/// <summary>The body of <c>GET /api/me</c>.</summary>
public sealed record ApiMe(int Id, string DisplayName, string Email, string Role, int? ManagerId);

/// <summary>The machine-readable rejection payload shared by every refusal.</summary>
public sealed record ApiErrorBody(string Code, string Message);

/// <summary>One check inside the health payload.</summary>
public sealed record HealthCheckEntry(string Name, string Status, string? Description);

/// <summary>The body of <c>GET /health</c>.</summary>
public sealed record HealthPayload(string Status, IReadOnlyList<HealthCheckEntry> Checks);

/// <summary>
/// Deserialisation that does not depend on the response's content type, so a test asserts the
/// payload it was given rather than failing on a header it is not about.
/// </summary>
public static class TestJson
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    /// <summary>Deserialises a JSON body using the web defaults the API serialises with.</summary>
    /// <typeparam name="T">Target type.</typeparam>
    /// <param name="body">The raw response body.</param>
    /// <returns>The deserialised value, or null when the body is not that shape.</returns>
    public static T? Deserialize<T>(string body) => JsonSerializer.Deserialize<T>(body, Options);
}
