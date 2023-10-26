using System.ComponentModel;
using ConversationalSearchPlatform.BackOffice.Services;

namespace ConversationalSearchPlatform.BackOffice.Jobs;

public class OpenAIPricingJob
{
    private readonly IOpenAIPriceFetchingService _priceFetchingService;
    private readonly IOpenAIPricingService _pricingService;

    public OpenAIPricingJob(IOpenAIPriceFetchingService priceFetchingService, IOpenAIPricingService pricingService)
    {
        _priceFetchingService = priceFetchingService;
        _pricingService = pricingService;
    }


    [DisplayName("Fetches Azure's OpenAI pricing and preheats the cache daily")]
    public async Task Execute() =>
        await _priceFetchingService.GetOpenAIPricingAsync(_pricingService.GetAllValidSkuNames());
}