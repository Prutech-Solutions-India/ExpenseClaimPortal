using System.Collections.Generic;

namespace PilotService.Data.Entities;

/// <summary>
/// A member of staff. Employees form a reporting line through <see cref="ManagerId"/>,
/// which is null for the top of the line.
/// </summary>
public class Employee
{
    /// <summary>Surrogate key. Seed ids are fixed so re-running the seed cannot duplicate rows.</summary>
    public int Id { get; set; }

    /// <summary>Name shown in the user switcher.</summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>Unique work email address.</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>Foreign key to the role this employee holds.</summary>
    public int RoleId { get; set; }

    /// <summary>The role this employee holds.</summary>
    public Role Role { get; set; } = null!;

    /// <summary>Foreign key to this employee's manager, or null at the top of the reporting line.</summary>
    public int? ManagerId { get; set; }

    /// <summary>This employee's manager, when one is set.</summary>
    public Employee? Manager { get; set; }

    /// <summary>Employees who report directly to this employee.</summary>
    public ICollection<Employee> Reports { get; set; } = new List<Employee>();
}
