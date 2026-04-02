using Microsoft.Extensions.Configuration;

namespace ReceiptReader.Services;

internal static class LogsDirectoryResolver
{
    public static string ResolveFromOptionalPath(string? configuredPath)
    {
        var trimmedPath = configuredPath?.Trim();

        if (string.IsNullOrWhiteSpace(trimmedPath))
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ReceiptReader", "Logs");
        }
        else
        {
            return trimmedPath;
        }
    }
}
