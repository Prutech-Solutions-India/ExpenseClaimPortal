using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using PilotService.Data;

namespace PilotService.Health;

/// <summary>
/// Reports whether the PostgreSQL database behind <see cref="AppDbContext"/> is reachable.
/// </summary>
/// <remarks>
/// The probe is what makes an unreachable database visible: start-up deliberately continues
/// when migrations fail, so without this check a host with no database would look identical
/// to a healthy one. The check opens a connection and runs a trivial query rather than only
/// inspecting configuration - a connection string that parses is not a database that answers.
/// </remarks>
public sealed class DatabaseHealthCheck : IHealthCheck
{
    /// <summary>Name the check is registered under, and the name that appears in the payload.</summary>
    public const string Name = "database";

    private readonly AppDbContext _dbContext;

    /// <summary>
    /// Creates the check.
    /// </summary>
    /// <param name="dbContext">Context whose connection is probed. Resolved per check execution.</param>
    public DatabaseHealthCheck(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <inheritdoc />
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var canConnect = await _dbContext.Database
                .CanConnectAsync(cancellationToken)
                .ConfigureAwait(false);

            if (!canConnect)
            {
                return HealthCheckResult.Unhealthy("The PostgreSQL database is not reachable.");
            }

            // CanConnectAsync can be satisfied by a pooled connection; a round trip proves the
            // server is still answering.
            await _dbContext.Database
                .ExecuteSqlRawAsync("SELECT 1", cancellationToken)
                .ConfigureAwait(false);

            return HealthCheckResult.Healthy("The PostgreSQL database is reachable.");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            return new HealthCheckResult(
                context.Registration.FailureStatus,
                description: "The PostgreSQL database is not reachable.",
                exception: exception,
                data: new Dictionary<string, object>
                {
                    ["error"] = exception.GetType().Name,
                });
        }
    }
}
