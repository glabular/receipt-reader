using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;

namespace ReceiptReader.Services;

internal sealed class BrowserEngine : IDisposable
{
    private readonly ChromeDriver _driver;
    private readonly TimeSpan _timeout;
    private bool _disposed;

    public BrowserEngine(TimeSpan? timeout = null)
    {
        _timeout = timeout ?? TimeSpan.FromSeconds(7);

        var options = new ChromeOptions();

        options.AddArgument("--headless");
        options.AddArgument("--disable-extensions");
        options.AddArgument("--blink-settings=imagesEnabled=false");

        _driver = new ChromeDriver(options);
    }

    public string GetPageSource(string url)
    {
        // Telegram pastes may leave a literal space in the crtd offset; Chrome needs it encoded.
        _driver.Navigate().GoToUrl(url.Replace(" ", "%20", StringComparison.Ordinal));

        // Wait for the page to load completely
        var wait = new WebDriverWait(_driver, _timeout);

        wait.Until(d => {
            var elements = d.FindElements(By.ClassName("invoice-items-list"));

            return elements.Count > 0 && elements[0].Displayed;
        });

        return _driver.PageSource;
    }

    public void Dispose()
    {
        if (_disposed)
        { 
            return; 
        }

        _driver.Quit();
        _driver.Dispose();
        _disposed = true;
    }
}
