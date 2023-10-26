namespace ConversationalSearchPlatform.BackOffice.Services.Models.Pricing;

public record CostCalculationResult
{
    public CostCalculationResult(decimal completionCost, decimal promptCost, decimal thousandUnitsPromptCost, decimal thousandUnitsCompletionCost)
    {
        CompletionCost = completionCost;
        PromptCost = promptCost;
        ThousandUnitsPromptCost = thousandUnitsPromptCost;
        ThousandUnitsCompletionCost = thousandUnitsCompletionCost;
    }

    public decimal CompletionCost { get; set; } = 0;
    public decimal PromptCost { get; set; } = 0;
    public decimal ThousandUnitsPromptCost { get; set; } = 0;
    public decimal ThousandUnitsCompletionCost { get; set; } = 0;
}