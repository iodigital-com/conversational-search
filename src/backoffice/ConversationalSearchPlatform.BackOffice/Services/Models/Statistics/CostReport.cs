using ConversationalSearchPlatform.BackOffice.Jobs.Models;

namespace ConversationalSearchPlatform.BackOffice.Services.Models.Statistics;

public record CostReport(
    Month Month,
    int WeekNumber,
    CallModel CallModel,
    CallType CallType,
    UsageType UsageType,
    decimal CompletionTokenSum,
    decimal PromptTokensSum,
    decimal TokenSum
);