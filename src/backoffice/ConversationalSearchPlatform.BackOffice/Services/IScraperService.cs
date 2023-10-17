using ConversationalSearchPlatform.BackOffice.Services.Models;

namespace ConversationalSearchPlatform.BackOffice.Services;

public interface IScraperService
{
    Task<ScrapeResult> ScrapeAsync(string url);
}