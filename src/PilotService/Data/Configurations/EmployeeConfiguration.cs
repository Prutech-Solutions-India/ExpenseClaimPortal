using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PilotService.Data.Entities;

namespace PilotService.Data.Configurations;

/// <summary>
/// Maps <see cref="Employee"/> onto the snake_case <c>employees</c> table, including the
/// role foreign key and the self-referencing reporting line.
/// </summary>
public class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
{
    public void Configure(EntityTypeBuilder<Employee> builder)
    {
        builder.ToTable("employees");

        builder.HasKey(employee => employee.Id)
            .HasName("pk_employees");

        builder.Property(employee => employee.Id)
            .HasColumnName("id")
            .UseIdentityByDefaultColumn();

        builder.Property(employee => employee.DisplayName)
            .HasColumnName("display_name")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(employee => employee.Email)
            .HasColumnName("email")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(employee => employee.RoleId)
            .HasColumnName("role_id")
            .IsRequired();

        builder.Property(employee => employee.ManagerId)
            .HasColumnName("manager_id");

        builder.HasIndex(employee => employee.Email)
            .IsUnique()
            .HasDatabaseName("ux_employees_email");

        builder.HasIndex(employee => employee.RoleId)
            .HasDatabaseName("ix_employees_role_id");

        builder.HasIndex(employee => employee.ManagerId)
            .HasDatabaseName("ix_employees_manager_id");

        builder.HasOne(employee => employee.Role)
            .WithMany(role => role.Employees)
            .HasForeignKey(employee => employee.RoleId)
            .HasConstraintName("fk_employees_roles_role_id")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(employee => employee.Manager)
            .WithMany(manager => manager.Reports)
            .HasForeignKey(employee => employee.ManagerId)
            .HasConstraintName("fk_employees_employees_manager_id")
            .OnDelete(DeleteBehavior.Restrict);
    }
}
