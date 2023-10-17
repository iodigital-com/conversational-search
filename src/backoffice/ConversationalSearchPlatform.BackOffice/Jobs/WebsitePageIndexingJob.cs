using ConversationalSearchPlatform.BackOffice.Data;
using ConversationalSearchPlatform.BackOffice.Services;
using ConversationalSearchPlatform.BackOffice.Services.Models;
using ConversationalSearchPlatform.BackOffice.Tenants;
using Finbuckle.MultiTenant;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace ConversationalSearchPlatform.BackOffice.Jobs;

public class WebsitePageIndexingJob : ITenantAwareIndexingJob<WebsitePageIndexingDetails>
{
    private readonly IDbContextFactory<ApplicationDbContext> _dbContextFactory;
    private readonly IMultiTenantContextAccessor<ApplicationTenantInfo> _tenantContextAccessor;
    private readonly IMultiTenantStore<ApplicationTenantInfo> _multiTenantStore;
    private readonly IScraperService _scraperService;
    private readonly IChunkService _chunkService;

    public WebsitePageIndexingJob(
        IDbContextFactory<ApplicationDbContext> dbContextFactory,
        IMultiTenantContextAccessor<ApplicationTenantInfo> tenantContextAccessor,
        IMultiTenantStore<ApplicationTenantInfo> multiTenantStore,
        IScraperService scraperService,
        IChunkService chunkService
    )
    {
        _dbContextFactory = dbContextFactory;
        _tenantContextAccessor = tenantContextAccessor;
        _multiTenantStore = multiTenantStore;
        _scraperService = scraperService;
        _chunkService = chunkService;
    }

    public async Task Execute(string tenantId, WebsitePageIndexingDetails details)
    {
        var tenant = await _multiTenantStore.TryGetAsync(tenantId);

        if (tenant == null)
        {
            Log.Error("Cannot find tenant {TenantId}", tenantId);
            return;
        }

        InitializeTenantInfo(tenant);

        using (var db = await _dbContextFactory.CreateDbContextAsync())
        {
            switch (details.ChangeType)
            {
                case IndexJobChangeType.CREATE:
                    await CreateEntry(db, tenantId, details);
                    break;
                case IndexJobChangeType.UPDATE:
                    break;
                case IndexJobChangeType.DELETE:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(details.ChangeType));
            }
        }
    }

    private async Task CreateEntry(ApplicationDbContext db, string tenantId, WebsitePageIndexingDetails details)
    {
        //TODO check tenant here, otherwise we need to manually set it before we try anything
        var websitePage = await db.WebsitePages.FirstOrDefaultAsync(page => page.Id == details.Id);
        var language = Language.English; //TODO this needs to be saved on the website page

        if (websitePage == null)
        {
            Log.Error(
                "Cannot find website page with id {WebsitePageId} for  {TenantId}",
                details.Id,
                tenantId
            );
            return;
        }

        // TODO test scraping the url
        var scrapeResult = await _scraperService.ScrapeAsync(websitePage.Url);
        // TODO chunk the page
        var chunkResult = await _chunkService.ChunkAsync(new ChunkInput(websitePage.Name, scrapeResult.HtmlContent, language));
        // TODO feed the page to vector db
        websitePage.IndexedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();
    }

    private void InitializeTenantInfo(ApplicationTenantInfo tenant)
    {
        if (_tenantContextAccessor.MultiTenantContext == null)
        {
            _tenantContextAccessor.MultiTenantContext = new MultiTenantContext<ApplicationTenantInfo>()
            {
                TenantInfo = tenant,
            };
        }
        else
        {
            _tenantContextAccessor.MultiTenantContext.TenantInfo = tenant;
        }
    }
}