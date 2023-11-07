namespace ConversationalSearchPlatform.BackOffice.Services.Models;

public record ScrapeResult(string HtmlContent, string PageTitle, List<ImageScrapePart> ImageScrapeParts);