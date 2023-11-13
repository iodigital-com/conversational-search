using ConversationalSearchPlatform.BackOffice.Services.Models.Sitemap;
using TurnerSoftware.SitemapTools;

namespace ConversationalSearchPlatform.BackOffice.Services;

public interface ISitemapParsingService
{
    Task<SitemapFile> ParseFromFileAsync(Stream xmlStream, CancellationToken cancellationToken = default);
    Task<SitemapFile> ParseFromUrlAsync(string url, CancellationToken cancellationToken = default);
    Task<SiteMapDiscoveryResult> DiscoverFromUrlAsync(string url, CancellationToken cancellationToken = default);
}