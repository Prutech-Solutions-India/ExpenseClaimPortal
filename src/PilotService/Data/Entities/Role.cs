using System.Collections.Generic;

namespace PilotService.Data.Entities;

/// <summary>
/// A role that an employee can hold. Valid codes are <c>Employee</c>, <c>Manager</c>
/// and <c>FinanceOfficer</c>; the rows themselves are supplied by Data/seed.sql.
/// </summary>
public class Role
{
    /// <summary>Surrogate key. Seeded ids are 1 (Employee), 2 (Manager) and 3 (FinanceOfficer).</summary>
    public int Id { get; set; }

    /// <summary>Stable machine-readable code placed on the wire, for example <c>FinanceOfficer</c>.</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>Human-readable label, for example <c>Finance Officer</c>.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Employees holding this role.</summary>
    public ICollection<Employee> Employees { get; set; } = new List<Employee>();
}
