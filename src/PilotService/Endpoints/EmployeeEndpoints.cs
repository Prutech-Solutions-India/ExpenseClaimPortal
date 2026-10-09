using System.Linq;
using System.Threading;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using PilotService.Contracts;
using PilotService.Data;
using PilotService.Identity;

namespace PilotService.Endpoints;

/// <summary>
/// Maps the staff list that backs the React user switcher.
/// </summary>
public static class EmployeeEndpoints
{
    /// <summary>
    /// Maps <c>GET /api/employees</c> onto the supplied <c>/api</c> group.
    /// </summary>
    /// <param name="api">The filtered <c>/api</c> route group.</param>
    /// <returns>The same group, so endpoint modules can be chained.</returns>
    /// <remarks>
    /// This endpoint is marked <see cref="AllowUnidentifiedExtensions.AllowUnidentified{TBuilder}"/>
    /// because the caller has to see the staff list before it can choose an acting identity.
    /// Every other endpoint in the group requires one. The response is a list of
    /// <see cref="EmployeeSummary"/>, projected in the query, so no EF entity reaches the wire.
    /// </remarks>
    public static RouteGroupBuilder MapEmployeeEndpoints(this RouteGroupBuilder api)
    {
        api.MapGet("/employees", async (AppDbContext dbContext, CancellationToken cancellationToken) =>
            {
                var employees = await dbContext.Employees
                    .AsNoTracking()
                    .OrderBy(employee => employee.DisplayName)
                    .Select(employee => new EmployeeSummary(
                        employee.Id,
                        employee.DisplayName,
                        employee.Role.Code,
                        employee.ManagerId))
                    .ToListAsync(cancellationToken)
                    .ConfigureAwait(false);

                return Results.Ok(employees);
            })
            .AllowUnidentified()
            .WithName("GetEmployees");

        return api;
    }
}
