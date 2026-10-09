using Microsoft.EntityFrameworkCore;
using PilotService.Data.Entities;

namespace PilotService.Data;

/// <summary>
/// EF Core context for the pilot. Mapping lives in the per-entity configuration classes
/// under <c>PilotService.Data.Configurations</c>; nothing here depends on attributes or
/// on convention-based naming.
/// </summary>
public class AppDbContext : DbContext
{
    /// <summary>Creates the context with the options supplied by dependency injection.</summary>
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    /// <summary>Staff records, including their reporting lines.</summary>
    public DbSet<Employee> Employees => Set<Employee>();

    /// <summary>Roles an employee can hold (Employee, Manager, FinanceOfficer).</summary>
    public DbSet<Role> Roles => Set<Role>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
