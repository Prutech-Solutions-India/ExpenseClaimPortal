using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace PilotService.Data.Migrations;

/// <summary>
/// Initial schema: the <c>roles</c> and <c>employees</c> tables with their indexes,
/// the role foreign key and the self-referencing reporting line.
/// </summary>
public partial class InitialCreate : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "roles",
            columns: table => new
            {
                id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                code = table.Column<string>(type: "text", nullable: false),
                name = table.Column<string>(type: "text", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_roles", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "employees",
            columns: table => new
            {
                id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                display_name = table.Column<string>(type: "text", nullable: false),
                email = table.Column<string>(type: "text", nullable: false),
                role_id = table.Column<int>(type: "integer", nullable: false),
                manager_id = table.Column<int>(type: "integer", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_employees", x => x.id);
                table.ForeignKey(
                    name: "fk_employees_employees_manager_id",
                    column: x => x.manager_id,
                    principalTable: "employees",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "fk_employees_roles_role_id",
                    column: x => x.role_id,
                    principalTable: "roles",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "ix_employees_manager_id",
            table: "employees",
            column: "manager_id");

        migrationBuilder.CreateIndex(
            name: "ix_employees_role_id",
            table: "employees",
            column: "role_id");

        migrationBuilder.CreateIndex(
            name: "ux_employees_email",
            table: "employees",
            column: "email",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ux_roles_code",
            table: "roles",
            column: "code",
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "employees");

        migrationBuilder.DropTable(
            name: "roles");
    }
}
