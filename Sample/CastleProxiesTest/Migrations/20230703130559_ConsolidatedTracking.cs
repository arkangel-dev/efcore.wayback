using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sample.Migrations
{
    /// <inheritdoc />
    public partial class ConsolidatedTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ChangeDate",
                table: "AuditEntries");

            migrationBuilder.DropColumn(
                name: "TransactionID",
                table: "AuditEntries");

            migrationBuilder.AddColumn<int>(
                name: "ParentTransactionID",
                table: "AuditEntries",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "AuditTransactions",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TransactionID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ChangeDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditTransactions", x => x.ID);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuditEntries_ParentTransactionID",
                table: "AuditEntries",
                column: "ParentTransactionID");

            migrationBuilder.AddForeignKey(
                name: "FK_AuditEntries_AuditTransactions_ParentTransactionID",
                table: "AuditEntries",
                column: "ParentTransactionID",
                principalTable: "AuditTransactions",
                principalColumn: "ID",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AuditEntries_AuditTransactions_ParentTransactionID",
                table: "AuditEntries");

            migrationBuilder.DropTable(
                name: "AuditTransactions");

            migrationBuilder.DropIndex(
                name: "IX_AuditEntries_ParentTransactionID",
                table: "AuditEntries");

            migrationBuilder.DropColumn(
                name: "ParentTransactionID",
                table: "AuditEntries");

            migrationBuilder.AddColumn<DateTime>(
                name: "ChangeDate",
                table: "AuditEntries",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "TransactionID",
                table: "AuditEntries",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }
    }
}
