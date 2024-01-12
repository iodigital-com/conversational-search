using ConversationalSearchPlatform.BackOffice.Constants;
using ConversationalSearchPlatform.BackOffice.Data;
using ConversationalSearchPlatform.BackOffice.Data.Entities;
using ConversationalSearchPlatform.BackOffice.Exceptions;
using ConversationalSearchPlatform.BackOffice.Jobs.Models;
using ConversationalSearchPlatform.BackOffice.Services;
using ConversationalSearchPlatform.BackOffice.Services.Models;
using ConversationalSearchPlatform.BackOffice.Services.Models.Weaviate.Queries;
using ConversationalSearchPlatform.BackOffice.Tenants;
using Finbuckle.MultiTenant;
using Hangfire;
using HtmlAgilityPack;
using HttpClientToCurl;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace ConversationalSearchPlatform.BackOffice.Jobs;

public class WebsitePageIndexingJob : ITenantAwareIndexingJob<WebsitePageIndexingDetails>
{
    private readonly IDbContextFactory<ApplicationDbContext> _dbContextFactory;
    private readonly IMultiTenantContextAccessor<ApplicationTenantInfo> _tenantContextAccessor;
    private readonly IMultiTenantStore<ApplicationTenantInfo> _multiTenantStore;
    private readonly IScraperService _scraperService;
    private readonly IChunkService _chunkService;
    private readonly IVectorizationService _vectorizationService;
    private readonly IAzureBlobStorage _azureBlobStorage;
    private readonly ISitemapParsingService _sitemapParsingService;
    private readonly IIndexingService<WebsitePage> _websitePageIndexingService;
    private readonly HttpClient _httpClient;
    private readonly ILogger<WebsitePageIndexingJob> _logger;

    public WebsitePageIndexingJob(IDbContextFactory<ApplicationDbContext> dbContextFactory,
        IMultiTenantContextAccessor<ApplicationTenantInfo> tenantContextAccessor,
        IMultiTenantStore<ApplicationTenantInfo> multiTenantStore,
        IScraperService scraperService,
        IChunkService chunkService,
        IVectorizationService vectorizationService,
        IAzureBlobStorage azureBlobStorage,
        ISitemapParsingService sitemapParsingService,
        IIndexingService<WebsitePage> websitePageIndexingService,
        HttpClient httpClient,
        ILogger<WebsitePageIndexingJob> logger)
    {
        _dbContextFactory = dbContextFactory;
        _tenantContextAccessor = tenantContextAccessor;
        _multiTenantStore = multiTenantStore;
        _scraperService = scraperService;
        _chunkService = chunkService;
        _vectorizationService = vectorizationService;
        _azureBlobStorage = azureBlobStorage;
        _sitemapParsingService = sitemapParsingService;
        _websitePageIndexingService = websitePageIndexingService;
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

        _tenantContextAccessor.InitializeForJob(tenant);

        using (var db = await _dbContextFactory.CreateDbContextAsync())
        {
            switch (details.ChangeType)
            {
                case IndexJobChangeType.CREATE:
                    await CreateEntry(db, tenantId, details);
                    break;
                case IndexJobChangeType.UPDATE:
                    await UpdateEntry(tenantId, details, db);
                    break;
                case IndexJobChangeType.DELETE:
                    await DeleteEntry(details);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(details.ChangeType));
            }
        }
    }

    private async Task UpdateEntry(string tenantId, WebsitePageIndexingDetails details, ApplicationDbContext db)
    {
        var websitePage = await db.WebsitePages.FirstOrDefaultAsync(page => page.Id == details.Id);

        //TODO implement update flow for every child
        if (websitePage != null && websitePage.IsValidSitemapParent())
        {
            
        }
        else
        {
            
        }

        await DeleteEntry(details);
        await CreateEntry(db, tenantId, details);
    }

    private async Task DeleteEntry(WebsitePageIndexingDetails details)
    {
        //TODO implement delete flow for every child
        await DeletePagesAsync(details.Id);
        await DeleteImagesAsync(details.Id);
    }

    private async Task DeletePagesAsync(Guid indexableId)
    {
        var recordsLeftToDelete = true;

        var request = GetWebsitePageByInternalIdForDeletion.Request(new GetWebsitePageByInternalIdForDeletion.GetByInternalIdForDeletionQueryParams(indexableId.ToString()));

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

        var request = GetImagesByInternalIdForDeletion.Request(new GetImagesByInternalIdForDeletion.GetImagesByInternalIdForDeletionQueryParams(indexable.ToString()));

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
            ThrowHelper.ThrowWebsitePageNotFoundException(details.Id);
            return;
        }

        if (websitePage.IsValidSitemapParent())
        {
            _logger.LogInformation("Found a SitemapParent record, will be creating child records and not processing this record any further");
            db.Entry(websitePage).State = EntityState.Detached;

            await CreateChildPages(_azureBlobStorage, _sitemapParsingService, _websitePageIndexingService, websitePage);
            return;
        }

        if (websitePage.Url == null)
        {
            _logger.LogError("Cannot parse this record as it has no url");
            ThrowHelper.ThrowInvalidWebsitePageUrl(websitePage.Id);
            return;
        }

        var scrapeResult = await _scraperService.ScrapeAsync(websitePage.Url);

        /*var chunkCollection = await _chunkService.ChunkAsync(
            new ChunkInput(
                websitePage.Id.ToString(),
                websitePage.Name,
                scrapeResult.HtmlContent,
                websitePage.Language,
                websitePage.Url,
                websitePage.ReferenceType,
                websitePage.TenantId
            )
        );*/

        var htmlDoc = new HtmlDocument();
        htmlDoc.LoadHtml(scrapeResult.HtmlContent);

        if (websitePage.ReferenceType == ReferenceType.Site)
        {
            List<ChunkResult> chunks = new List<ChunkResult>();

            var nodes = htmlDoc.DocumentNode.SelectNodes("//div[contains(@class, 'section')]");

            foreach (var node in nodes)
            {
                var cleanText = Regex.Replace(node.InnerText, @"\s+", " ").Trim();
                cleanText = WebUtility.HtmlDecode(cleanText);

                if (!string.IsNullOrEmpty(cleanText))
                {
                    var chunkResult = new ChunkResult();
                    chunkResult.ArticleNumber = string.Empty;
                    chunkResult.Text = cleanText;
                    chunkResult.Packaging = string.Empty;

                    chunks.Add(chunkResult);
                }
            }

            if (chunks.Count > 0)
            {
                ChunkCollection chunkCollection = new ChunkCollection(tenantId, websitePage.Id.ToString(), websitePage.Url, websitePage.ReferenceType.ToString(), websitePage.Language.ToString(), chunks);

                await _vectorizationService.BulkCreateAsync(nameof(WebsitePage), websitePage.Id, scrapeResult.PageTitle, tenantId, UsageType.Indexing, chunkCollection);
            }
        } 
        else
        {
            var productText = new StringBuilder();
            var packageText = "no package info";
            var titleText = string.Empty;
            var articleNumberText = "no article number";
            var descriptionNodes = htmlDoc.DocumentNode.SelectNodes("//*[contains(@class, 'redesignProductheadline') or contains(@class, 'description-short') or contains(@class, 'ContentSlideWrap') or contains(@class, 'ProductDescription')]");
            if (descriptionNodes != null)
            {
                foreach (var node in descriptionNodes)
                {
                    var cleanText = Regex.Replace(node.InnerText, @"\s+", " ").Trim();
                    cleanText = WebUtility.HtmlDecode(cleanText);
                    productText.Append(cleanText);
                    productText.Append(" ");

                    if (titleText == "")
                    {
                        titleText = cleanText;
                    }
                }
            }

            var packageNodes = htmlDoc.DocumentNode.SelectNodes("//td[@data-label='Volume' or @data-label='Pieces' or @data-label='Pcs/Case']");
            var packageBuilder = new StringBuilder();
            if (packageNodes != null)
            {
                foreach (var pnode in packageNodes) 
                {
                    var cleanText = Regex.Replace(pnode.InnerText, @"\s+", " ").Trim();

                    packageBuilder.Append($"{pnode.GetAttributeValue("data-label", "")} {cleanText};");
                }
            }

            if (packageBuilder.Length > 0)
            {
                packageText = packageBuilder.ToString();
            }

            var articleNumberNodes = htmlDoc.DocumentNode.SelectNodes("//td[@data-label='Article #']");

            if (articleNumberNodes?.Count > 0)
            {
                articleNumberText = articleNumberNodes[0].InnerText;
            }

            var chunkResult = new ChunkResult();
            chunkResult.ArticleNumber = articleNumberText;
            chunkResult.Text = productText.ToString().Trim();
            chunkResult.Packaging = packageText.Trim();

            if (!string.IsNullOrEmpty(chunkResult.Text))
            {
                ChunkCollection chunkCollection = new ChunkCollection(tenantId, websitePage.Id.ToString(), websitePage.Url, websitePage.ReferenceType.ToString(), websitePage.Language.ToString(), new List<ChunkResult>() { chunkResult });

                await _vectorizationService.BulkCreateAsync(nameof(WebsitePage), websitePage.Id, titleText, tenantId, UsageType.Indexing, chunkCollection);
            }
        }

        websitePage.IndexedAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync();
    }


    private static async Task CreateChildPages(
        IAzureBlobStorage azureBlobStorage,
        ISitemapParsingService sitemapParsingService,
        IIndexingService<WebsitePage> websitePageIndexingService,
        WebsitePage parentPage)
    {
        var blob = await azureBlobStorage.DownloadAsync(parentPage.SitemapFileName!);
        var sitemap = await sitemapParsingService.ParseFromFileAsync(blob?.Content!);

        var childPages = sitemap.Urls
            .Select(sitemapEntry => sitemapEntry.Location)
            .Select(uri => new WebsitePage(
                    parentPage.Name,
                    uri.ToString(),
                    parentPage.ReferenceType,
                    parentPage.Language,
                    false,
                    null,
                    null)
                {
                    ParentId = parentPage.Id,
                    // Parent = parentPage
                }
            )
            .ToList();

        await websitePageIndexingService.CreateBulkAsync(childPages);
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
}