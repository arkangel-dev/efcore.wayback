using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sample.Migrations
{
    /// <inheritdoc />
    public partial class AddedNewAudit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AuditEntries_Messages_MessageID",
                table: "AuditEntries");

            migrationBuilder.DropForeignKey(
                name: "FK_AuditEntries_Users_UserID",
                table: "AuditEntries");

            migrationBuilder.DropIndex(
                name: "IX_AuditEntries_MessageID",
                table: "AuditEntries");

            migrationBuilder.DropIndex(
                name: "IX_AuditEntries_UserID",
                table: "AuditEntries");

            migrationBuilder.DropColumn(
                name: "MessageID",
                table: "AuditEntries");

            migrationBuilder.DropColumn(
                name: "UserID",
                table: "AuditEntries");

            migrationBuilder.AddColumn<string>(
                name: "TableName",
                table: "AuditEntries",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TableName",
                table: "AuditEntries");

            migrationBuilder.AddColumn<int>(
                name: "MessageID",
                table: "AuditEntries",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UserID",
                table: "AuditEntries",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AuditEntries_MessageID",
                table: "AuditEntries",
                column: "MessageID");

            migrationBuilder.CreateIndex(
                name: "IX_AuditEntries_UserID",
                table: "AuditEntries",
                column: "UserID");

            migrationBuilder.AddForeignKey(
                name: "FK_AuditEntries_Messages_MessageID",
                table: "AuditEntries",
                column: "MessageID",
                principalTable: "Messages",
                principalColumn: "ID");

            migrationBuilder.AddForeignKey(
                name: "FK_AuditEntries_Users_UserID",
                table: "AuditEntries",
                column: "UserID",
                principalTable: "Users",
                principalColumn: "ID");
        }
    }
}
