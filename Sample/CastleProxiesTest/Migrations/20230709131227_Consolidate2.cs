using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sample.Migrations
{
    /// <inheritdoc />
    public partial class Consolidate2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "J1Table",
                table: "AuditEntries");

            migrationBuilder.DropColumn(
                name: "J2Table",
                table: "AuditEntries");

            migrationBuilder.DropColumn(
                name: "PropertyName",
                table: "AuditEntries");

            migrationBuilder.DropColumn(
                name: "TableName",
                table: "AuditEntries");

            migrationBuilder.AddColumn<int>(
                name: "J1TableID",
                table: "AuditEntries",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "J2TableID",
                table: "AuditEntries",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PropertyID",
                table: "AuditEntries",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TableID",
                table: "AuditEntries",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "AuditTables",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditTables", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "AuditProperties",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ParentTableID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditProperties", x => x.ID);
                    table.ForeignKey(
                        name: "FK_AuditProperties_AuditTables_ParentTableID",
                        column: x => x.ParentTableID,
                        principalTable: "AuditTables",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuditEntries_J1TableID",
                table: "AuditEntries",
                column: "J1TableID");

            migrationBuilder.CreateIndex(
                name: "IX_AuditEntries_J2TableID",
                table: "AuditEntries",
                column: "J2TableID");

            migrationBuilder.CreateIndex(
                name: "IX_AuditEntries_PropertyID",
                table: "AuditEntries",
                column: "PropertyID");

            migrationBuilder.CreateIndex(
                name: "IX_AuditEntries_TableID",
                table: "AuditEntries",
                column: "TableID");

            migrationBuilder.CreateIndex(
                name: "IX_AuditProperties_ParentTableID",
                table: "AuditProperties",
                column: "ParentTableID");

            migrationBuilder.AddForeignKey(
                name: "FK_AuditEntries_AuditProperties_PropertyID",
                table: "AuditEntries",
                column: "PropertyID",
                principalTable: "AuditProperties",
                principalColumn: "ID");

            migrationBuilder.AddForeignKey(
                name: "FK_AuditEntries_AuditTables_J1TableID",
                table: "AuditEntries",
                column: "J1TableID",
                principalTable: "AuditTables",
                principalColumn: "ID");

            migrationBuilder.AddForeignKey(
                name: "FK_AuditEntries_AuditTables_J2TableID",
                table: "AuditEntries",
                column: "J2TableID",
                principalTable: "AuditTables",
                principalColumn: "ID");

            migrationBuilder.AddForeignKey(
                name: "FK_AuditEntries_AuditTables_TableID",
                table: "AuditEntries",
                column: "TableID",
                principalTable: "AuditTables",
                principalColumn: "ID",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AuditEntries_AuditProperties_PropertyID",
                table: "AuditEntries");

            migrationBuilder.DropForeignKey(
                name: "FK_AuditEntries_AuditTables_J1TableID",
                table: "AuditEntries");

            migrationBuilder.DropForeignKey(
                name: "FK_AuditEntries_AuditTables_J2TableID",
                table: "AuditEntries");

            migrationBuilder.DropForeignKey(
                name: "FK_AuditEntries_AuditTables_TableID",
                table: "AuditEntries");

            migrationBuilder.DropTable(
                name: "AuditProperties");

            migrationBuilder.DropTable(
                name: "AuditTables");

            migrationBuilder.DropIndex(
                name: "IX_AuditEntries_J1TableID",
                table: "AuditEntries");

            migrationBuilder.DropIndex(
                name: "IX_AuditEntries_J2TableID",
                table: "AuditEntries");

            migrationBuilder.DropIndex(
                name: "IX_AuditEntries_PropertyID",
                table: "AuditEntries");

            migrationBuilder.DropIndex(
                name: "IX_AuditEntries_TableID",
                table: "AuditEntries");

            migrationBuilder.DropColumn(
                name: "J1TableID",
                table: "AuditEntries");

            migrationBuilder.DropColumn(
                name: "J2TableID",
                table: "AuditEntries");

            migrationBuilder.DropColumn(
                name: "PropertyID",
                table: "AuditEntries");

            migrationBuilder.DropColumn(
                name: "TableID",
                table: "AuditEntries");

            migrationBuilder.AddColumn<string>(
                name: "J1Table",
                table: "AuditEntries",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "J2Table",
                table: "AuditEntries",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PropertyName",
                table: "AuditEntries",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TableName",
                table: "AuditEntries",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }
    }
}
