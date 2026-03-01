using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReceiptReader.Migrations
{
    /// <inheritdoc />
    public partial class ChangeForeignKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Invoices_TelegramUsers_TelegramUserId",
                table: "Invoices");

            migrationBuilder.RenameColumn(
                name: "TelegramUserId",
                table: "Invoices",
                newName: "UserId");

            migrationBuilder.RenameIndex(
                name: "IX_Invoices_TelegramUserId",
                table: "Invoices",
                newName: "IX_Invoices_UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Invoices_TelegramUsers_UserId",
                table: "Invoices",
                column: "UserId",
                principalTable: "TelegramUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Invoices_TelegramUsers_UserId",
                table: "Invoices");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "Invoices",
                newName: "TelegramUserId");

            migrationBuilder.RenameIndex(
                name: "IX_Invoices_UserId",
                table: "Invoices",
                newName: "IX_Invoices_TelegramUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Invoices_TelegramUsers_TelegramUserId",
                table: "Invoices",
                column: "TelegramUserId",
                principalTable: "TelegramUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
