using ReceiptReader.Models;
using ReceiptReader.Services;

namespace ReceiptReader;

internal sealed class ReceiptClient : IDisposable
{
    private readonly BrowserEngine _browser;

    public ReceiptClient(TimeSpan? browserTimeout = null)
    {
        _browser = new BrowserEngine(browserTimeout);
    }

    public InvoiceResult GetInvoice(string url)
    {
        var pageSource = _browser.GetPageSource(url);

        return InvoiceParser.ParseInvoicePage(pageSource);
    }

    public void Dispose() => throw new NotImplementedException();
}
