namespace ConversationalSearchPlatform.BackOffice.Services.Models.Sitemap;

public record SiteMapDiscoveryResult(IEnumerable<Uri> SiteMaps)
{
    public bool DiscoveredAny() => SiteMaps.Any();
};