using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReceiptReader.Migrations
{
    /// <inheritdoc />
    public partial class AddUserNamesToTelegramUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FirstName",
                table: "TelegramUsers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastName",
                table: "TelegramUsers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Username",
                table: "TelegramUsers",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FirstName",
                table: "TelegramUsers");

            migrationBuilder.DropColumn(
                name: "LastName",
                table: "TelegramUsers");

            migrationBuilder.DropColumn(
                name: "Username",
                table: "TelegramUsers");
        }
    }
}
