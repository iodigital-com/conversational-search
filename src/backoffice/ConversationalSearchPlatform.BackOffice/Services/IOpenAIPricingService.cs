using ConversationalSearchPlatform.BackOffice.Jobs;
using ConversationalSearchPlatform.BackOffice.Jobs.Models;
using ConversationalSearchPlatform.BackOffice.Services.Models.Pricing;

namespace ConversationalSearchPlatform.BackOffice.Services;

public interface IOpenAIPricingService
{
    List<string> GetAllValidSkuNames();
    IEnumerable<string> GetSkuNamesForCallModel(CallModel callModel);
    string GetSkuNameForCallModelAndCostType(CallModel callModel, CostType costType);
    ValueTask<List<AzurePricingItem>> GetAzurePricingItemsAsync();
}