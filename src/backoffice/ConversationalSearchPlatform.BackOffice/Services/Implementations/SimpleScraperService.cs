using ConversationalSearchPlatform.BackOffice.Services.Models;
using HtmlAgilityPack;

namespace ConversationalSearchPlatform.BackOffice.Services.Implementations;

public class SimpleScraperService : BaseScraper, IScraperService
{
    private readonly HttpClient _httpClient;

    public SimpleScraperService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<ScrapeResult> ScrapeAsync(string url)
    {
        var stream = await _httpClient.GetStreamAsync(url);

        var htmlDoc = new HtmlDocument();
        htmlDoc.Load(stream);

        var imageScrapeParts = GetImageScrapeParts(htmlDoc);

        var html = htmlDoc.DocumentNode.OuterHtml;
        return new ScrapeResult(html ?? string.Empty, imageScrapeParts);
    }
}