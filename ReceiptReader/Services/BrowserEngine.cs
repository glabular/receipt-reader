using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;

namespace ReceiptReader.Services;

internal sealed class BrowserEngine
{
    public static string GetPageSource(string url)
    {
        // HtmlAgilityPack parses static HTML only;
        // use a headless browser if JavaScript-rendered content must load first.
        var options = new ChromeOptions();

        options.AddArgument("--headless");
        options.AddArgument("--disable-extensions");
        options.AddArgument("--blink-settings=imagesEnabled=false");

        using var driver = new ChromeDriver(options);

        driver.Navigate().GoToUrl(url);

        // Wait for the page to load completely
        var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(5));

        wait.Until(d => {
            var elements = d.FindElements(By.ClassName("invoice-items-list"));

            return elements.Count > 0 && elements[0].Displayed;
        });

        return driver.PageSource;
    }
}
