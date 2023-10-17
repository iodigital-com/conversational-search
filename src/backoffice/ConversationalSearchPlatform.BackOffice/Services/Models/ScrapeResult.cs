namespace ConversationalSearchPlatform.BackOffice.Services.Models;

public record ScrapeResult(string HtmlContent, List<ImageScrapePart> ImageScrapeParts);