using ConversationalSearchPlatform.BackOffice.Services.Models.Pricing;

namespace ConversationalSearchPlatform.BackOffice.Services;

public interface IOpenAIPriceFetchingService
{
    Task<List<AzurePricingItem>> GetOpenAIPricingAsync(List<string> skuNames);
}