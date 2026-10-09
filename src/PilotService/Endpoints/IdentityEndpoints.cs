using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using PilotService.Contracts;
using PilotService.Identity;

namespace PilotService.Endpoints;

/// <summary>
/// Maps the endpoint that reports who the server decided the caller is.
/// </summary>
public static class IdentityEndpoints
{
    /// <summary>
    /// Maps <c>GET /api/me</c> onto the supplied <c>/api</c> group.
    /// </summary>
    /// <param name="api">The filtered <c>/api</c> route group.</param>
    /// <returns>The same group, so endpoint modules can be chained.</returns>
    /// <remarks>
    /// The identity comes from <see cref="ICurrentUserAccessor"/>, which
    /// <see cref="ActingIdentityEndpointFilter"/> populated from the employee row. The handler
    /// never reads the request header itself, so what it returns is always what the server
    /// resolved rather than what the browser asserted.
    /// </remarks>
    public static RouteGroupBuilder MapIdentityEndpoints(this RouteGroupBuilder api)
    {
        api.MapGet("/me", (ICurrentUserAccessor currentUserAccessor) =>
            {
                var user = currentUserAccessor.Require();

                return Results.Ok(new MeResponse(
                    user.Id,
                    user.DisplayName,
                    user.Email,
                    user.Role,
                    user.ManagerId));
            })
            .WithName("GetMe");

        return api;
    }
}
