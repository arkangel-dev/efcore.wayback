using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sample.Migrations
{
    /// <inheritdoc />
    public partial class ChangedDBName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Junction_Interests_Users_Users_UserID",
                table: "Junction_Interests_Users");

            migrationBuilder.DropForeignKey(
                name: "FK_Messages_Users_RecipientID",
                table: "Messages");

            migrationBuilder.DropForeignKey(
                name: "FK_Messages_Users_SenderID",
                table: "Messages");

            migrationBuilder.DropForeignKey(
                name: "FK_Users_Users_BestFriendID",
                table: "Users");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Users",
                table: "Users");

            migrationBuilder.RenameTable(
                name: "Users",
                newName: "UserL");

            migrationBuilder.RenameIndex(
                name: "IX_Users_BestFriendID",
                table: "UserL",
                newName: "IX_UserL_BestFriendID");

            migrationBuilder.AddPrimaryKey(
                name: "PK_UserL",
                table: "UserL",
                column: "ID");

            migrationBuilder.AddForeignKey(
                name: "FK_Junction_Interests_Users_UserL_UserID",
                table: "Junction_Interests_Users",
                column: "UserID",
                principalTable: "UserL",
                principalColumn: "ID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Messages_UserL_RecipientID",
                table: "Messages",
                column: "RecipientID",
                principalTable: "UserL",
                principalColumn: "ID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Messages_UserL_SenderID",
                table: "Messages",
                column: "SenderID",
                principalTable: "UserL",
                principalColumn: "ID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_UserL_UserL_BestFriendID",
                table: "UserL",
                column: "BestFriendID",
                principalTable: "UserL",
                principalColumn: "ID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Junction_Interests_Users_UserL_UserID",
                table: "Junction_Interests_Users");

            migrationBuilder.DropForeignKey(
                name: "FK_Messages_UserL_RecipientID",
                table: "Messages");

            migrationBuilder.DropForeignKey(
                name: "FK_Messages_UserL_SenderID",
                table: "Messages");

            migrationBuilder.DropForeignKey(
                name: "FK_UserL_UserL_BestFriendID",
                table: "UserL");

            migrationBuilder.DropPrimaryKey(
                name: "PK_UserL",
                table: "UserL");

            migrationBuilder.RenameTable(
                name: "UserL",
                newName: "Users");

            migrationBuilder.RenameIndex(
                name: "IX_UserL_BestFriendID",
                table: "Users",
                newName: "IX_Users_BestFriendID");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Users",
                table: "Users",
                column: "ID");

            migrationBuilder.AddForeignKey(
                name: "FK_Junction_Interests_Users_Users_UserID",
                table: "Junction_Interests_Users",
                column: "UserID",
                principalTable: "Users",
                principalColumn: "ID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Messages_Users_RecipientID",
                table: "Messages",
                column: "RecipientID",
                principalTable: "Users",
                principalColumn: "ID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Messages_Users_SenderID",
                table: "Messages",
                column: "SenderID",
                principalTable: "Users",
                principalColumn: "ID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Users_BestFriendID",
                table: "Users",
                column: "BestFriendID",
                principalTable: "Users",
                principalColumn: "ID");
        }
    }
}
