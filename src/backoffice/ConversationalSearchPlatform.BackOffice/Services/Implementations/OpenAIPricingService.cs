using ConversationalSearchPlatform.BackOffice.Constants;
using ConversationalSearchPlatform.BackOffice.Jobs.Models;
using ConversationalSearchPlatform.BackOffice.Services.Models;
using ConversationalSearchPlatform.BackOffice.Services.Models.Pricing;
using Microsoft.Extensions.Caching.Memory;

namespace ConversationalSearchPlatform.BackOffice.Services.Implementations;

public class OpenAIPricingService : IOpenAIPricingService
{
    private readonly IMemoryCache _memoryCache;
    private readonly IOpenAIPriceFetchingService _priceFetchingService;

    public OpenAIPricingService(IMemoryCache memoryCache, IOpenAIPriceFetchingService priceFetchingService)
    {
        _memoryCache = memoryCache;
        _priceFetchingService = priceFetchingService;
    }

    private readonly List<KeyValuePair<CallModel, Sku>> _callModelMapping = new()
    {
        new KeyValuePair<CallModel, Sku>(CallModel.Gpt35Turbo, new Sku("GPT-35-turbo-4k-Prompt", CostType.Prompt)),
        new KeyValuePair<CallModel, Sku>(CallModel.Gpt35Turbo, new Sku("GPT-35-turbo-4k-Completion", CostType.Completion)),
        new KeyValuePair<CallModel, Sku>(CallModel.Gpt35Turbo_16K, new Sku("GPT-35-turbo-16k-Prompt", CostType.Prompt)),
        new KeyValuePair<CallModel, Sku>(CallModel.Gpt35Turbo_16K, new Sku("GPT-35-turbo-16k-Prompt", CostType.Completion)),
        new KeyValuePair<CallModel, Sku>(CallModel.Gpt4, new Sku("GPT4-8K-Prompt", CostType.Prompt)),
        new KeyValuePair<CallModel, Sku>(CallModel.Gpt4, new Sku("GPT4-8K-Completion", CostType.Completion)),
        new KeyValuePair<CallModel, Sku>(CallModel.Gpt4_32K, new Sku("GPT4-32K-Prompt", CostType.Prompt)),
        new KeyValuePair<CallModel, Sku>(CallModel.Gpt4_32K, new Sku("GPT4-32K-Completion", CostType.Completion)),
        new KeyValuePair<CallModel, Sku>(CallModel.AdaTextEmbedding, new Sku("Embeddings-Ada", CostType.Embedding))
    };

    public async ValueTask<List<AzurePricingItem>> GetAzurePricingItemsAsync() =>
        await _memoryCache.GetOrCreateAsync<List<AzurePricingItem>>(
            PricingConstants.PricingCacheKey,
            async (_) => await _priceFetchingService.GetOpenAIPricingAsync(GetAllValidSkuNames())) ??
        throw new InvalidOperationException();

    public List<string> GetAllValidSkuNames()
    {
        var skuNames = new List<string>();

        foreach (CallModel model in Enum.GetValuesAsUnderlyingType<CallModel>())
        {
            var matchingSkuNames = GetSkuNamesForCallModel(model);
            skuNames.AddRange(matchingSkuNames);
        }

        return skuNames;
    }

    public IEnumerable<string> GetSkuNamesForCallModel(CallModel callModel) =>
        _callModelMapping.Where(pair => pair.Key == callModel).Select(pair => pair.Value.Name);

    public string GetSkuNameForCallModelAndCostType(CallModel callModel, CostType costType)
    {
        var pair = _callModelMapping.First(pair => pair.Key == callModel && pair.Value.CostType == costType);
        return pair.Value.Name;
    }
}