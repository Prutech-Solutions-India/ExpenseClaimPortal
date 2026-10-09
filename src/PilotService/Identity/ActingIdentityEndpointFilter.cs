using System.Globalization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PilotService.Contracts;
using PilotService.Data;

namespace PilotService.Identity;

/// <summary>
/// Resolves the acting employee for every request that reaches the <c>/api</c> group, or
/// refuses the request with a machine-readable payload. This is the only place that reads
/// <see cref="ActingIdentity.HeaderName"/>: handlers receive the resolved identity through
/// <see cref="ICurrentUserAccessor"/>, so authorisation cannot be skipped by hiding a control
/// in the UI.
/// </summary>
/// <remarks>
/// Endpoints carrying <see cref="AllowUnidentifiedMetadata"/> pass straight through. Everything
/// else is rejected with 401 when no usable identifier was supplied and 403 when the supplied
/// identifier matches no employee row.
/// </remarks>
public sealed class ActingIdentityEndpointFilter : IEndpointFilter
{
    private readonly ILogger<ActingIdentityEndpointFilter> _logger;

    /// <summary>
    /// Creates the filter. Only request-independent services are taken here; the scoped
    /// <see cref="AppDbContext"/> and <see cref="CurrentUserAccessor"/> are resolved from the
    /// request services inside <see cref="InvokeAsync"/>, because an endpoint filter instance
    /// may outlive a single request scope.
    /// </summary>
    /// <param name="logger">Logger used to record refused requests.</param>
    public ActingIdentityEndpointFilter(ILogger<ActingIdentityEndpointFilter> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var httpContext = context.HttpContext;
        var endpoint = httpContext.GetEndpoint();

        if (endpoint?.Metadata.GetMetadata<AllowUnidentifiedMetadata>() is not null)
        {
            return await next(context).ConfigureAwait(false);
        }

        var headerValue = httpContext.Request.Headers[ActingIdentity.HeaderName].ToString();

        if (string.IsNullOrWhiteSpace(headerValue)
            || !int.TryParse(headerValue.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var employeeId)
            || employeeId <= 0)
        {
            _logger.LogWarning(
                "Rejecting {Method} {Path}: the {HeaderName} header was missing or not a positive integer.",
                httpContext.Request.Method,
                httpContext.Request.Path.Value,
                ActingIdentity.HeaderName);

            return Results.Json(
                ApiError.ActingIdentityMissing(),
                statusCode: StatusCodes.Status401Unauthorized);
        }

        var services = httpContext.RequestServices;
        var dbContext = services.GetRequiredService<AppDbContext>();

        var employee = await dbContext.Employees
            .AsNoTracking()
            .Include(candidate => candidate.Role)
            .FirstOrDefaultAsync(candidate => candidate.Id == employeeId, httpContext.RequestAborted)
            .ConfigureAwait(false);

        if (employee is null)
        {
            _logger.LogWarning(
                "Rejecting {Method} {Path}: employee {EmployeeId} does not exist.",
                httpContext.Request.Method,
                httpContext.Request.Path.Value,
                employeeId);

            return Results.Json(
                ApiError.ActingIdentityUnknown(employeeId),
                statusCode: StatusCodes.Status403Forbidden);
        }

        var accessor = services.GetRequiredService<CurrentUserAccessor>();
        accessor.Set(new CurrentUser(
            employee.Id,
            employee.DisplayName,
            employee.Email,
            employee.Role.Code,
            employee.ManagerId));

        return await next(context).ConfigureAwait(false);
    }
}
