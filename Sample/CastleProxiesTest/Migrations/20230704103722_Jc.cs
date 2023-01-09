using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sample.Migrations
{
    /// <inheritdoc />
    public partial class Jc : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "J1",
                table: "AuditEntries",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "J1Table",
                table: "AuditEntries",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "J2",
                table: "AuditEntries",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "J2Table",
                table: "AuditEntries",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "J1",
                table: "AuditEntries");

            migrationBuilder.DropColumn(
                name: "J1Table",
                table: "AuditEntries");

            migrationBuilder.DropColumn(
                name: "J2",
                table: "AuditEntries");

            migrationBuilder.DropColumn(
                name: "J2Table",
                table: "AuditEntries");
        }
    }
}
