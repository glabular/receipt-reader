using ReceiptReader.Models;
using ReceiptReader.Services;

namespace ReceiptReader;

internal sealed class ReceiptClient : IDisposable
{
    // TODO: Think on adding a global semaphore (e.g. max 1–2 concurrent browsers)
    private readonly BrowserEngine _browser;

    public ReceiptClient(TimeSpan? browserTimeout = null)
    {
        _browser = new BrowserEngine(browserTimeout);
    }

    public Invoice? GetInvoice(string url)
    {
        var pageSource = _browser.GetPageSource(url);

        return InvoiceParser.ParseInvoicePage(url, pageSource);
    }

    public void Dispose() => _browser.Dispose();
}
