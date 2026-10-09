namespace PilotService.Contracts;

/// <summary>
/// One entry of the staff list returned by <c>GET /api/employees</c>. This is the shape the
/// browser sees; the EF entity never reaches the wire, so the database model can change
/// without breaking the user switcher.
/// </summary>
/// <param name="Id">Employee identifier, sent back as the acting identity header value.</param>
/// <param name="DisplayName">Name shown in the switcher.</param>
/// <param name="Role">Role code: <c>Employee</c>, <c>Manager</c> or <c>FinanceOfficer</c>.</param>
/// <param name="ManagerId">Identifier of this employee's manager, or null at the top of the reporting line.</param>
public sealed record EmployeeSummary(
    int Id,
    string DisplayName,
    string Role,
    int? ManagerId);
