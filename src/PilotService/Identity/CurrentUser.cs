namespace PilotService.Identity;

/// <summary>
/// The acting employee for the current request, resolved server-side from the employee row
/// the acting identity header pointed at. Handlers read their caller's role from here, so a
/// browser cannot claim a role simply by asking for one.
/// </summary>
/// <param name="Id">Identifier of the resolved employee.</param>
/// <param name="DisplayName">Name of the resolved employee.</param>
/// <param name="Email">Work email address of the resolved employee.</param>
/// <param name="Role">Role code: <c>Employee</c>, <c>Manager</c> or <c>FinanceOfficer</c>.</param>
/// <param name="ManagerId">Identifier of the resolved employee's manager, or null at the top of the reporting line.</param>
public sealed record CurrentUser(
    int Id,
    string DisplayName,
    string Email,
    string Role,
    int? ManagerId);
