using ReceiptReader.Models;
using ReceiptReader.Services;

namespace ReceiptReader;

internal sealed class ReceiptClient : IDisposable
{
    private const int MaxConcurrentBrowsers = 2;
    private static readonly SemaphoreSlim BrowserSemaphore = new(MaxConcurrentBrowsers, MaxConcurrentBrowsers);
    private readonly BrowserEngine _browser;

    public ReceiptClient(TimeSpan? browserTimeout = null)
    {
        _browser = new BrowserEngine(browserTimeout);
    }

    public async Task<Invoice?> GetInvoiceAsync(string url, CancellationToken cancellationToken = default)
    {
        await BrowserSemaphore.WaitAsync(cancellationToken);

        try
        {
            var pageSource = _browser.GetPageSource(url);

            return InvoiceParser.ParseInvoicePage(url, pageSource);
        }
        finally
        {
            BrowserSemaphore.Release();
        }
    }

    public void Dispose() => _browser.Dispose();
}
