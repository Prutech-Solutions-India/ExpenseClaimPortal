using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace PilotService.Health;

/// <summary>
/// Serialises a <see cref="HealthReport"/> as JSON so a probe can read the overall status and
/// the per-check detail without parsing prose. The HTTP status code is chosen by the health
/// check middleware options; this writer only produces the body.
/// </summary>
/// <remarks>
/// Shape: <c>{ "status": "Healthy", "checks": [ { "name", "status", "description" } ] }</c>.
/// </remarks>
public static class HealthResponseWriter
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// Writes the report to the response body as JSON.
    /// </summary>
    /// <param name="context">The request being answered.</param>
    /// <param name="report">The aggregated health report.</param>
    public static Task WriteAsync(HttpContext context, HealthReport report)
    {
        var checks = report.Entries
            .Select(entry => new HealthCheckEntryResponse(
                entry.Key,
                entry.Value.Status.ToString(),
                entry.Value.Description ?? entry.Value.Exception?.Message))
            .OrderBy(entry => entry.Name)
            .ToList();

        var payload = new HealthResponse(report.Status.ToString(), checks);

        return context.Response.WriteAsJsonAsync(payload, SerializerOptions, context.RequestAborted);
    }
}

/// <summary>The health payload returned by the probe endpoint.</summary>
/// <param name="Status">Aggregate status: <c>Healthy</c>, <c>Degraded</c> or <c>Unhealthy</c>.</param>
/// <param name="Checks">One entry per registered check.</param>
public sealed record HealthResponse(
    string Status,
    IReadOnlyList<HealthCheckEntryResponse> Checks);

/// <summary>The result of a single registered health check.</summary>
/// <param name="Name">Name the check was registered under, for example <c>database</c>.</param>
/// <param name="Status">Status of this check.</param>
/// <param name="Description">Human-readable detail, or the failure message when one was thrown.</param>
public sealed record HealthCheckEntryResponse(
    string Name,
    string Status,
    string? Description);
