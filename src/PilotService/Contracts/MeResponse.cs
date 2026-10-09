namespace PilotService.Contracts;

/// <summary>
/// The acting identity as resolved on the server, returned by <c>GET /api/me</c>. The value
/// comes from the employee row the request header pointed at, never from anything the
/// browser asserted about itself.
/// </summary>
/// <param name="Id">Identifier of the resolved employee.</param>
/// <param name="DisplayName">Name of the resolved employee.</param>
/// <param name="Email">Work email address of the resolved employee.</param>
/// <param name="Role">Role code: <c>Employee</c>, <c>Manager</c> or <c>FinanceOfficer</c>.</param>
/// <param name="ManagerId">Identifier of the resolved employee's manager, or null at the top of the reporting line.</param>
public sealed record MeResponse(
    int Id,
    string DisplayName,
    string Email,
    string Role,
    int? ManagerId);
