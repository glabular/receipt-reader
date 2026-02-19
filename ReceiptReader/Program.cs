using HtmlAgilityPack;

namespace ReceiptReader;

internal class Program
{
    static async Task Main(string[] args)
    {
        while (true)
        {
			try
			{
                Console.WriteLine("Enter a URL to fetch the title:");
                var url = Console.ReadLine()?.Trim();
                using var client = new HttpClient();
                var html = await client.GetStringAsync(url);
                var doc = new HtmlDocument();

                doc.LoadHtml(html);

                var title = doc.DocumentNode.SelectSingleNode("//title")?.InnerText;

                Console.WriteLine(title);
                Console.WriteLine("---");
            }
			catch (Exception ex)
			{
                Console.WriteLine("Something went wrong:");
                Console.WriteLine(ex.Message);
			}
        }        
    }
}
