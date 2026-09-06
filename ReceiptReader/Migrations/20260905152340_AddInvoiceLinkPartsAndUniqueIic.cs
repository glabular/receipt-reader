using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReceiptReader.Migrations
{
    /// <inheritdoc />
    public partial class AddInvoiceLinkPartsAndUniqueIic : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Invoices_URL",
                table: "Invoices");

            migrationBuilder.AlterColumn<string>(
                name: "URL",
                table: "Invoices",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AddColumn<string>(
                name: "BusinessUnitCode",
                table: "Invoices",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CashRegisterCode",
                table: "Invoices",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InvoiceIdentificationCode",
                table: "Invoices",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OrdinalNumber",
                table: "Invoices",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SoftwareCode",
                table: "Invoices",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TaxpayerIdentificationNumber",
                table: "Invoices",
                type: "nvarchar(max)",
                nullable: true);

            // Reads current Id/URL rows and queues UPDATEs after the AddColumn operations above.
            InvoiceLinkPartsBackfill.QueueUpdatesFromExistingUrls(migrationBuilder);

            migrationBuilder.Sql(
                """
                IF EXISTS (SELECT 1 FROM [Invoices] WHERE [InvoiceIdentificationCode] IS NULL OR [TaxpayerIdentificationNumber] IS NULL)
                BEGIN
                    THROW 50001, 'Invoice link backfill left required columns NULL. Fix unparseable URLs and retry.', 1;
                END

                IF EXISTS (
                    SELECT 1
                    FROM [Invoices]
                    GROUP BY [InvoiceIdentificationCode]
                    HAVING COUNT(*) > 1)
                BEGIN
                    THROW 50002, 'Invoice link backfill found duplicate InvoiceIdentificationCode values.', 1;
                END
                """);

            migrationBuilder.AlterColumn<string>(
                name: "InvoiceIdentificationCode",
                table: "Invoices",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "TaxpayerIdentificationNumber",
                table: "Invoices",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_InvoiceIdentificationCode",
                table: "Invoices",
                column: "InvoiceIdentificationCode",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Invoices_InvoiceIdentificationCode",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "BusinessUnitCode",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "CashRegisterCode",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "InvoiceIdentificationCode",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "OrdinalNumber",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "SoftwareCode",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "TaxpayerIdentificationNumber",
                table: "Invoices");

            migrationBuilder.AlterColumn<string>(
                name: "URL",
                table: "Invoices",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_URL",
                table: "Invoices",
                column: "URL",
                unique: true);
        }
    }
}
