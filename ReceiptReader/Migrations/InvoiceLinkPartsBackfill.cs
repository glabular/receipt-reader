using System.Globalization;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using ReceiptReader.Models;

namespace ReceiptReader.Migrations;

/// <summary>
/// Queues UPDATE statements that fill Invoice link columns from stored URLs.
/// Runs while building the migration operation list (before those SQLs execute),
/// so it reads Id/URL from the current schema and appends updates after AddColumn ops.
/// </summary>
internal static class InvoiceLinkPartsBackfill
{
    public static void QueueUpdatesFromExistingUrls(MigrationBuilder migrationBuilder)
    {
        var connectionString = ResolveConnectionString();
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Cannot backfill invoice link parts: connection string 'DefaultConnection' is missing. " +
                "Ensure user secrets (or configuration) are available when running " +
                "'dotnet ef database update'.");
        }

        using var connection = new SqlConnection(connectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = "SELECT [Id], [URL] FROM [Invoices]";

        var failedIds = new List<int>();
        var iicOwners = new Dictionary<string, int>(StringComparer.Ordinal);

        using (var reader = command.ExecuteReader())
        {
            while (reader.Read())
            {
                var id = reader.GetInt32(0);
                var url = reader.GetString(1);

                if (!TaxReceiptLink.TryParse(url, out var link))
                {
                    failedIds.Add(id);
                    continue;
                }

                if (iicOwners.TryGetValue(link.InvoiceIdentificationCode, out var otherId))
                {
                    throw new InvalidOperationException(
                        $"Cannot backfill invoice link parts: duplicate IIC " +
                        $"'{link.InvoiceIdentificationCode}' on invoices {otherId} and {id}.");
                }

                iicOwners[link.InvoiceIdentificationCode] = id;
                migrationBuilder.Sql(BuildUpdateSql(id, link));
            }
        }

        if (failedIds.Count > 0)
        {
            throw new InvalidOperationException(
                "Cannot backfill invoice link parts: TaxReceiptLink.TryParse failed for invoice Id(s): " +
                string.Join(", ", failedIds) +
                ". Fix those URLs (or remove the rows) and run 'dotnet ef database update' again.");
        }
    }

    private static string BuildUpdateSql(int id, TaxReceiptLink link)
    {
        return string.Format(
            CultureInfo.InvariantCulture,
            """
            UPDATE [Invoices] SET
                [InvoiceIdentificationCode] = {0},
                [TaxpayerIdentificationNumber] = {1},
                [OrdinalNumber] = {2},
                [BusinessUnitCode] = {3},
                [CashRegisterCode] = {4},
                [SoftwareCode] = {5}
            WHERE [Id] = {6};
            """,
            SqlNString(link.InvoiceIdentificationCode),
            SqlNString(link.TaxpayerIdentificationNumber),
            link.OrdinalNumber is int ord ? ord.ToString(CultureInfo.InvariantCulture) : "NULL",
            SqlNString(link.BusinessUnitCode),
            SqlNString(link.CashRegisterCode),
            SqlNString(link.SoftwareCode),
            id);
    }

    private static string SqlNString(string? value)
    {
        if (value is null)
        {
            return "NULL";
        }

        return "N'" + value.Replace("'", "''", StringComparison.Ordinal) + "'";
    }

    private static string? ResolveConnectionString()
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
            .AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: false)
            .AddUserSecrets<Program>(optional: true)
            .Build();

        return configuration.GetConnectionString("DefaultConnection");
    }
}
