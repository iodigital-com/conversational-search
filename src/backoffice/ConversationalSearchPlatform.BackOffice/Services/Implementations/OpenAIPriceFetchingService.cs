using System.Text.Json.Serialization;
using ConversationalSearchPlatform.BackOffice.Bootstrap;
using ConversationalSearchPlatform.BackOffice.Constants;
using ConversationalSearchPlatform.BackOffice.Exceptions;
using ConversationalSearchPlatform.BackOffice.Services.Models.Pricing;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace ConversationalSearchPlatform.BackOffice.Services.Implementations;

public class OpenAIPriceFetchingService : IOpenAIPriceFetchingService
{
    private readonly IMemoryCache _memoryCache;
    private readonly AzurePricingSettings _azurePricingSettings;
    private readonly ILogger<OpenAIPriceFetchingService> _logger;
    private readonly HttpClient _client;

    public OpenAIPriceFetchingService(
        IMemoryCache memoryCache,
        IOptions<AzurePricingSettings> azurePricingSettings,
        ILogger<OpenAIPriceFetchingService> logger,
        HttpClient client)
    {
        _memoryCache = memoryCache;
        _azurePricingSettings = azurePricingSettings.Value;
        _logger = logger;
        _client = client;
    }

    public async Task<List<AzurePricingItem>> GetOpenAIPricingAsync(List<string> skuNames)
    {
        var pricingItemsForRegion = await GetAsyncInternal(skuNames);
        _memoryCache.Set(PricingConstants.PricingCacheKey, pricingItemsForRegion);
        return pricingItemsForRegion;
    }

    private async Task<List<AzurePricingItem>> GetAsyncInternal(List<string> skuNames)
    {
        var url = "/api/retail/prices?currencyCode=%27EUR%27&$filter=productName%20eq%20%27Azure%20OpenAI%27";

        var items = await FetchDataPagedAsync(url);

        var regionName = _azurePricingSettings.RegionName;

        var pricingItemsForRegion = GetPricingItemsForRegion(skuNames, items, regionName);
        return pricingItemsForRegion;
    }

    private async Task<List<AzurePricingItem>> FetchDataPagedAsync(string url)
    {
        var items = new List<AzurePricingItem>();

        var pricingResponse = await FetchDataAsync(url);

        items.AddRange(pricingResponse.Items);

        var nextPageLink = pricingResponse.NextPageLink;

        // Fetch all pages, not only first page
        while (!string.IsNullOrWhiteSpace(nextPageLink))
        {
            var uri = new Uri(nextPageLink);
            var host = uri.Host;

            url = nextPageLink.Replace(host, string.Empty);
            pricingResponse = await FetchDataAsync(url);

            items.AddRange(pricingResponse.Items);
            nextPageLink = pricingResponse.NextPageLink;
        }

        return items;
    }


    private List<AzurePricingItem> GetPricingItemsForRegion(List<string> skuNames, List<AzurePricingItem> items, string regionName)
    {
        var pricingItemsForRegion = new List<AzurePricingItem>();

        foreach (var skuName in skuNames)
        {
            var matching = items.FirstOrDefault(item => item.ArmRegionName == regionName && item.ArmSkuName == skuName);

            if (matching == null)
            {
                _logger.LogDebug("Cannot find matching record for {SkuName}", skuName);
            }
            else
            {
                pricingItemsForRegion.Add(matching);
            }
        }

        return pricingItemsForRegion;
    }

    private async Task<AzurePricingResponse> FetchDataAsync(string url)
    {
        var httpResponseMessage = await _client.GetAsync(url);
        httpResponseMessage.EnsureSuccessStatusCode();

        var pricingResponse = await httpResponseMessage.Content.ReadFromJsonAsync<AzurePricingResponse>();

        if (pricingResponse == null)
        {
            throw new AzurePricingNotFetchableException($"Unable to fetch Azure pricing information on {DateTimeOffset.UtcNow}");
        }

        return pricingResponse;
    }
}