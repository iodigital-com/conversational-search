using ConversationalSearchPlatform.BackOffice.Data;
using ConversationalSearchPlatform.BackOffice.Jobs.Models;
using ConversationalSearchPlatform.BackOffice.Services;
using ConversationalSearchPlatform.BackOffice.Services.Models.Pricing;
using Microsoft.EntityFrameworkCore;

namespace ConversationalSearchPlatform.BackOffice.Jobs;

public class OpenAICallExecutedHandler(
    IDbContextFactory<ApplicationDbContext> dbContextFactory,
    ILogger<OpenAICallExecutedHandler> logger,
    IOpenAIPricingService pricingService
)
{
    public async Task Handle(OpenAICallExecutedEvent @event)
    {
        try
        {
            using (var db = await dbContextFactory.CreateDbContextAsync())
            {
                var azurePricingItems = await pricingService.GetAzurePricingItemsAsync();

                var calculationResult = CalculateCosts(@event, azurePricingItems);

                var record = new OpenAIConsumption(
                    @event.CorrelationId,
                    @event.TenantId,
                    (ExecutionType)@event.UsageType,
                    (ConsumptionModel)@event.CallModel,
                    (ConsumptionType)@event.CallType,
                    @event.CompletionTokens,
                    @event.PromptTokens,
                    @event.ExecutedAt,
                    calculationResult.ThousandUnitsPromptCost,
                    calculationResult.ThousandUnitsCompletionCost,
                    calculationResult.PromptCost,
                    calculationResult.CompletionCost
                );
                db.Set<OpenAIConsumption>().Add(record);
                await db.SaveChangesAsync();
            }
        }
        catch (Exception e)
        {
            logger.LogError(e, "Unable to save consumption for event {Event}", @event.ToString());
            throw;
        }
    }

    private CostCalculationResult CalculateCosts(
        OpenAICallExecutedEvent @event,
        List<AzurePricingItem> azurePricingItems
    )
    {
        decimal completionCost = 0;
        decimal promptCost = 0;
        decimal thousandUnitsPromptCost = 0;
        decimal thousandUnitsCompletionCost = 0;

        switch (@event.CallType)
        {
            case CallType.GPT when @event.UsageType == UsageType.Conversation:
            {
                var promptCostSku = pricingService.GetSkuNameForCallModelAndCostType(@event.CallModel, CostType.Prompt);
                thousandUnitsPromptCost = GetPricingBySku(azurePricingItems, promptCostSku).UnitPrice;
                promptCost = Calculate(thousandUnitsPromptCost, @event.PromptTokens);

                var completionCostSku = pricingService.GetSkuNameForCallModelAndCostType(@event.CallModel, CostType.Completion);
                thousandUnitsCompletionCost = GetPricingBySku(azurePricingItems, completionCostSku).UnitPrice;
                completionCost = Calculate(thousandUnitsCompletionCost, @event.CompletionTokens);
                break;
            }
            case CallType.Embedding:
            {
                var embeddingCostSku = pricingService.GetSkuNameForCallModelAndCostType(@event.CallModel, CostType.Embedding);
                thousandUnitsPromptCost = GetPricingBySku(azurePricingItems, embeddingCostSku).UnitPrice;
                //TODO check what kind of tokens we get on ADA, completion or prompt
                promptCost = Calculate(thousandUnitsPromptCost, @event.PromptTokens);
                break;
            }
            default:
                throw new ArgumentOutOfRangeException(nameof(@event.CallType));
        }

        return new CostCalculationResult(
            completionCost,
            promptCost,
            thousandUnitsPromptCost,
            thousandUnitsCompletionCost
        );
    }

    private static AzurePricingItem GetPricingBySku(List<AzurePricingItem> azurePricingItems, string promptCostSku) =>
        azurePricingItems.First(item => item.ArmSkuName == promptCostSku);

    private static decimal Calculate(decimal unitPriceForModel, decimal usedTokens)
    {
        var unitPrice = unitPriceForModel / 1000;
        return unitPrice * usedTokens;
    }
}