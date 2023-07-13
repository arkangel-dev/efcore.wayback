using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sample.Migrations
{
    /// <inheritdoc />
    public partial class BestFriend : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BestFriendID",
                table: "Users",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_BestFriendID",
                table: "Users",
                column: "BestFriendID");

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Users_BestFriendID",
                table: "Users",
                column: "BestFriendID",
                principalTable: "Users",
                principalColumn: "ID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Users_Users_BestFriendID",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_BestFriendID",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "BestFriendID",
                table: "Users");
        }
    }
}
