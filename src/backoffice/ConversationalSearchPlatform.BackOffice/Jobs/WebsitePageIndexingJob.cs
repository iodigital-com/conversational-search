using ConversationalSearchPlatform.BackOffice.Constants;
using ConversationalSearchPlatform.BackOffice.Data;
using ConversationalSearchPlatform.BackOffice.Data.Entities;
using ConversationalSearchPlatform.BackOffice.Jobs.Models;
using ConversationalSearchPlatform.BackOffice.Services;
using ConversationalSearchPlatform.BackOffice.Services.Models;
using ConversationalSearchPlatform.BackOffice.Services.Models.Weaviate.Queries;
using ConversationalSearchPlatform.BackOffice.Tenants;
using Finbuckle.MultiTenant;
using Hangfire;
using HttpClientToCurl;
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
    private readonly IVectorizationService _vectorizationService;
    private readonly HttpClient _httpClient;
    private readonly ILogger<WebsitePageIndexingJob> _logger;

    public WebsitePageIndexingJob(
        IDbContextFactory<ApplicationDbContext> dbContextFactory,
        IMultiTenantContextAccessor<ApplicationTenantInfo> tenantContextAccessor,
        IMultiTenantStore<ApplicationTenantInfo> multiTenantStore,
        IScraperService scraperService,
        IChunkService chunkService,
        IVectorizationService vectorizationService,
        HttpClient httpClient,
        ILogger<WebsitePageIndexingJob> logger)
    {
        _dbContextFactory = dbContextFactory;
        _tenantContextAccessor = tenantContextAccessor;
        _multiTenantStore = multiTenantStore;
        _scraperService = scraperService;
        _chunkService = chunkService;
        _vectorizationService = vectorizationService;
        _httpClient = httpClient;
        _logger = logger;
    }

    [AutomaticRetry(Attempts = 0, OnAttemptsExceeded = AttemptsExceededAction.Fail)]
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
                    await DeleteEntry(details);
                    await CreateEntry(db, tenantId, details);
                    break;
                case IndexJobChangeType.DELETE:
                    await DeleteEntry(details);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(details.ChangeType));
            }
        }
    }

    private async Task DeleteEntry(WebsitePageIndexingDetails details)
    {
        await DeletePagesAsync(details.Id);
        await DeleteImagesAsync(details.Id);
    }

    private async Task DeletePagesAsync(Guid indexableId)
    {
        var recordsLeftToDelete = true;

        var request = new GetWebsitePageByInternalIdForDeletion()
            .Request(new GetWebsitePageByInternalIdForDeletion.GetByInternalIdForDeletionQueryParams(indexableId.ToString()));

        do
        {
            var pageResponses = await _vectorizationService
                .SearchAsync<
                    GetWebsitePageByInternalIdForDeletion.GetByInternalIdForDeletionQueryParams,
                    GetWebsitePageByInternalIdForDeletion.WeaviateRecordResponse
                >(nameof(WebsitePage), request);


            if (pageResponses.Count == 0)
            {
                recordsLeftToDelete = false;
            }
            else
            {
                var pageIdsToDelete = pageResponses.Select(response => response.Additional?.Id)
                    .Where(guid => guid != null)
                    .OfType<Guid>()
                    .ToList();
                await _vectorizationService.BulkDeleteAsync(nameof(WebsitePage), pageIdsToDelete);
            }
        } while (recordsLeftToDelete);
    }

    private async Task DeleteImagesAsync(Guid indexable)
    {
        var recordsLeftToDelete = true;

        var request = new GetImagesByInternalIdForDeletion()
            .Request(new GetImagesByInternalIdForDeletion.GetImagesByInternalIdForDeletionQueryParams(indexable.ToString()));

        do
        {
            var imageResponses = await _vectorizationService
                .SearchAsync<
                    GetImagesByInternalIdForDeletion.GetImagesByInternalIdForDeletionQueryParams,
                    GetImagesByInternalIdForDeletion.WeaviateRecordResponse
                >(IndexingConstants.ImageClass, request);

            if (imageResponses.Count == 0)
            {
                recordsLeftToDelete = false;
            }
            else
            {
                var imageIdsToDelete = imageResponses.Select(response => response.Additional?.Id)
                    .Where(guid => guid != null)
                    .OfType<Guid>()
                    .ToList();

                await _vectorizationService.BulkDeleteAsync(IndexingConstants.ImageClass, imageIdsToDelete);
            }
        } while (recordsLeftToDelete);
    }

    private async Task CreateEntry(ApplicationDbContext db, string tenantId, WebsitePageIndexingDetails details)
    {
        var websitePage = await db.WebsitePages.FirstOrDefaultAsync(page => page.Id == details.Id);

        if (websitePage == null)
        {
            Log.Error(
                "Cannot find website page with id {WebsitePageId} for  {TenantId}",
                details.Id,
                tenantId
            );
            return;
        }

        var scrapeResult = await _scraperService.ScrapeAsync(websitePage.Url);

        var chunkCollection = await _chunkService.ChunkAsync(
            new ChunkInput(
                websitePage.Id.ToString(),
                websitePage.Name,
                scrapeResult.HtmlContent,
                websitePage.Language,
                websitePage.Url,
                websitePage.ReferenceType,
                websitePage.TenantId
            )
        );
        await _vectorizationService.BulkCreateAsync(nameof(WebsitePage), websitePage.Id, tenantId, UsageType.Indexing, chunkCollection);

        var imageCollection = await GetImageCollection(websitePage.Id, scrapeResult);
        await _vectorizationService.BulkCreateAsync(IndexingConstants.ImageClass, websitePage.Id, tenantId, UsageType.Indexing, imageCollection);


        websitePage.IndexedAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync();
    }

    private async Task<ImageCollection> GetImageCollection(Guid websitePageId, ScrapeResult scrapeResult)
    {
        var internalId = websitePageId.ToString();

        var imageResults = new List<ImageResult>();

        foreach (var part in scrapeResult.ImageScrapeParts)
        {
            try
            {
                var filename = Path.GetFileName(new Uri(part.Url).AbsolutePath);

                var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, part.Url);
                httpRequestMessage.Headers.TryAddWithoutValidation("Accept", "*/*");
                httpRequestMessage.Headers.UserAgent.TryParseAdd("insomnia/8.3.0");
#if DEBUG
                _httpClient.GenerateCurlInConsole(httpRequestMessage);
#endif
                var response = await _httpClient.SendAsync(httpRequestMessage);

                if (response.IsSuccessStatusCode)
                {
                    var byteArray = await response.Content.ReadAsByteArrayAsync();
                    var imageBlob = Convert.ToBase64String(byteArray);

                    var imageResult = new ImageResult(filename, imageBlob, part.AltDescription, part.NearbyText, part.Url, internalId);
                    imageResults.Add(imageResult);
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Unable to scrape image {}", part.Url);
            }
        }

        return new ImageCollection(internalId, imageResults);
    }

    private void InitializeTenantInfo(ApplicationTenantInfo tenant)
    {
        if (_tenantContextAccessor.MultiTenantContext == null)
        {
            _tenantContextAccessor.MultiTenantContext = new MultiTenantContext<ApplicationTenantInfo>
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