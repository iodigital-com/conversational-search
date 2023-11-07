using ConversationalSearchPlatform.BackOffice.Services.Models;
using HtmlAgilityPack;

namespace ConversationalSearchPlatform.BackOffice.Services.Implementations;

public class PuppeteerScraperService : BaseScraper, IScraperService
{
    private readonly HttpClient _httpClient;

    public PuppeteerScraperService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<ScrapeResult> ScrapeAsync(string url)
    {
        var stream = await _httpClient.GetStreamAsync($"scrape?url={url}");
        var htmlDoc = new HtmlDocument();
        htmlDoc.Load(stream);

        var imageScrapeParts = GetImageScrapeParts(htmlDoc);
        var pageTitle = GetPageTitle(htmlDoc);

        var html = htmlDoc.DocumentNode.OuterHtml;
        return new ScrapeResult(
            html ?? string.Empty,
            pageTitle ?? string.Empty,
            imageScrapeParts
        );
    }


}