using ConversationalSearchPlatform.BackOffice.Jobs.Models;

namespace ConversationalSearchPlatform.BackOffice.Services.Models.Statistics;

public record ConversationCostReport(Guid ConversationId,
    Month Month,
    CallModel CallModel,
    int WeekNumber,
    decimal PromptTokenCostSum,
    decimal CompletionCostSum,
    decimal TotalCostSum);