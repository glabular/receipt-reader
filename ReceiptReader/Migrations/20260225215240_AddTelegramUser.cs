using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReceiptReader.Migrations
{
    /// <inheritdoc />
    public partial class AddTelegramUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TelegramUserId",
                table: "Invoices",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "TelegramUsers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TelegramUserId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TelegramUsers", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_TelegramUserId",
                table: "Invoices",
                column: "TelegramUserId");

            migrationBuilder.CreateIndex(
                name: "IX_TelegramUsers_TelegramUserId",
                table: "TelegramUsers",
                column: "TelegramUserId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Invoices_TelegramUsers_TelegramUserId",
                table: "Invoices",
                column: "TelegramUserId",
                principalTable: "TelegramUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Invoices_TelegramUsers_TelegramUserId",
                table: "Invoices");

            migrationBuilder.DropTable(
                name: "TelegramUsers");

            migrationBuilder.DropIndex(
                name: "IX_Invoices_TelegramUserId",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "TelegramUserId",
                table: "Invoices");
        }
    }
}
