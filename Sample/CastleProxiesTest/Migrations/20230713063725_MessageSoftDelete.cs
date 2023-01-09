using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sample.Migrations
{
    /// <inheritdoc />
    public partial class MessageSoftDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Junction_Interests_Users");

            migrationBuilder.AddColumn<DateTime>(
                name: "DeleteDate",
                table: "Messages",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeleteDate",
                table: "Messages");

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Junction_Interests_Users",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }
    }
}
