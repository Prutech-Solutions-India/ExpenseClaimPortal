using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace PilotService.Data;

/// <summary>
/// Brings the database up to date at start-up: applies the EF Core migrations and then
/// executes the idempotent <c>Data/seed.sql</c> script, so a fresh database needs no
/// manual step.
/// </summary>
/// <remarks>
/// Failures are logged at <see cref="LogLevel.Critical"/> and swallowed on purpose. An
/// unreachable database must still leave a running host, because the health endpoint is
/// how that condition is reported - a host that refuses to start reports nothing at all.
/// </remarks>
public static class DatabaseInitializer
{
    /// <summary>Directory holding the seed script, relative to a probed root.</summary>
    private const string SeedDirectoryName = "Data";

    /// <summary>File name of the seed script.</summary>
    private const string SeedFileName = "seed.sql";

    /// <summary>Project-relative path used when probing parent directories of a run from source.</summary>
    private static readonly string[] ProjectRelativeSegments = { "src", "PilotService", SeedDirectoryName, SeedFileName };

    /// <summary>How far up the directory tree the seed script is looked for.</summary>
    private const int MaxParentProbeDepth = 8;

    /// <summary>
    /// Runs the initialiser using the services of the supplied scope.
    /// </summary>
    /// <param name="scope">A service scope owning a scoped <see cref="AppDbContext"/>.</param>
    /// <param name="cancellationToken">Token observed while migrating and seeding.</param>
    public static Task RunAsync(IServiceScope scope, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(scope);

        return RunAsync(scope.ServiceProvider, cancellationToken);
    }

    /// <summary>
    /// Applies migrations and seed data using the supplied (scoped) service provider.
    /// </summary>
    /// <param name="services">Provider able to resolve <see cref="AppDbContext"/>.</param>
    /// <param name="cancellationToken">Token observed while migrating and seeding.</param>
    public static async Task RunAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(services);

        ILogger logger = services.GetService<ILoggerFactory>()?.CreateLogger("PilotService.Data.DatabaseInitializer")
            ?? NullLogger.Instance;

        try
        {
            var context = services.GetRequiredService<AppDbContext>();

            await context.Database.MigrateAsync(cancellationToken).ConfigureAwait(false);
            logger.LogInformation("Database migrations applied.");

            var contentRootPath = services.GetService<IHostEnvironment>()?.ContentRootPath;
            var seedPath = ResolveSeedScriptPath(contentRootPath);

            if (seedPath is null)
            {
                logger.LogWarning(
                    "Seed script {SeedFile} was not found; the schema is up to date but no starting rows were written.",
                    Path.Combine(SeedDirectoryName, SeedFileName));
                return;
            }

            var sql = await File.ReadAllTextAsync(seedPath, cancellationToken).ConfigureAwait(false);

            if (string.IsNullOrWhiteSpace(sql))
            {
                logger.LogWarning("Seed script {SeedPath} is empty; nothing to apply.", seedPath);
                return;
            }

            await context.Database.ExecuteSqlRawAsync(sql, cancellationToken).ConfigureAwait(false);
            logger.LogInformation("Seed data applied from {SeedPath}.", seedPath);
        }
        catch (Exception exception)
        {
            logger.LogCritical(
                exception,
                "Database initialisation failed. The host will continue to start and the health endpoint will report unhealthy.");
        }
    }

    /// <summary>
    /// Locates <c>Data/seed.sql</c>, first beside the compiled output, then under the
    /// content root, then by walking parent directories for a run from source.
    /// </summary>
    /// <param name="contentRootPath">The host content root, when one is available.</param>
    /// <returns>The full path of the seed script, or <c>null</c> when it cannot be found.</returns>
    public static string? ResolveSeedScriptPath(string? contentRootPath)
    {
        foreach (var candidate in EnumerateCandidatePaths(contentRootPath))
        {
            if (File.Exists(candidate))
            {
                return Path.GetFullPath(candidate);
            }
        }

        return null;
    }

    /// <summary>Yields every location the seed script is looked for, in priority order.</summary>
    private static IEnumerable<string> EnumerateCandidatePaths(string? contentRootPath)
    {
        var baseDirectory = AppContext.BaseDirectory;

        yield return Path.Combine(baseDirectory, SeedDirectoryName, SeedFileName);

        if (!string.IsNullOrWhiteSpace(contentRootPath))
        {
            yield return Path.Combine(contentRootPath, SeedDirectoryName, SeedFileName);
            yield return Path.Combine(contentRootPath, Path.Combine(ProjectRelativeSegments));
        }

        foreach (var root in EnumerateProbeRoots(baseDirectory, contentRootPath))
        {
            var directory = new DirectoryInfo(root);

            for (var depth = 0; depth < MaxParentProbeDepth && directory is not null; depth++)
            {
                yield return Path.Combine(directory.FullName, SeedDirectoryName, SeedFileName);
                yield return Path.Combine(directory.FullName, Path.Combine(ProjectRelativeSegments));

                directory = directory.Parent;
            }
        }
    }

    /// <summary>Yields the directories whose ancestry is worth probing.</summary>
    private static IEnumerable<string> EnumerateProbeRoots(string baseDirectory, string? contentRootPath)
    {
        yield return baseDirectory;

        if (!string.IsNullOrWhiteSpace(contentRootPath)
            && !string.Equals(Path.GetFullPath(contentRootPath), Path.GetFullPath(baseDirectory), StringComparison.Ordinal))
        {
            yield return contentRootPath;
        }
    }
}
