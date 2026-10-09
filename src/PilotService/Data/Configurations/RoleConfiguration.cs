using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PilotService.Data.Entities;

namespace PilotService.Data.Configurations;

/// <summary>
/// Maps <see cref="Role"/> onto the snake_case <c>roles</c> table. Mapping lives here
/// rather than on the entity so the entity stays a plain object.
/// </summary>
public class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("roles");

        builder.HasKey(role => role.Id)
            .HasName("pk_roles");

        builder.Property(role => role.Id)
            .HasColumnName("id")
            .UseIdentityByDefaultColumn();

        builder.Property(role => role.Code)
            .HasColumnName("code")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(role => role.Name)
            .HasColumnName("name")
            .HasColumnType("text")
            .IsRequired();

        builder.HasIndex(role => role.Code)
            .IsUnique()
            .HasDatabaseName("ux_roles_code");
    }
}
