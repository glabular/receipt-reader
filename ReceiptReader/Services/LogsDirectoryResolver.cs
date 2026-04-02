using Microsoft.Extensions.Configuration;

namespace ReceiptReader.Services;

internal static class LogsDirectoryResolver
{
    internal const string ConfigurationKey = "ReceiptReader:LogsDirectory";

    public static string Resolve(IConfiguration configuration)
    {
        var configured = configuration[ConfigurationKey];

        if (!string.IsNullOrWhiteSpace(configured))
        {
            return Path.GetFullPath(configured);
        }

        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ReceiptReader",
            "Logs");
    }
}
