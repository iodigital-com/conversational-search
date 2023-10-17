using ConversationalSearchPlatform.BackOffice.Services.Models;
using HtmlAgilityPack;

namespace ConversationalSearchPlatform.BackOffice.Services.Implementations;

public class SimpleScraperService : IScraperService
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
        return new ScrapeResult(url, imageScrapeParts);
    }

    private static List<ImageScrapePart> GetImageScrapeParts(HtmlDocument htmlDoc)
    {
        var imageScrapeParts = new List<ImageScrapePart>();
        var nodes = htmlDoc.DocumentNode.SelectNodes("//img");

        if (nodes == null)
            return imageScrapeParts;

        foreach (var node in nodes)
        {
            var src = node.GetAttributeValue("src", null);
            var alt = node.GetAttributeValue("alt", null);
            imageScrapeParts.Add(new ImageScrapePart(src, alt));
        }

        return imageScrapeParts;
    }
}