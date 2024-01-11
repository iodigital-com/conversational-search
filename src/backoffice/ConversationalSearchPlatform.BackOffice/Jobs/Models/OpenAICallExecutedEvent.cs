namespace ConversationalSearchPlatform.BackOffice.Jobs.Models;

public record OpenAICallExecutedEvent(Guid CorrelationId,
    string TenantId,
    CallType CallType,
    CallModel CallModel,
    UsageType UsageType,
    int CompletionTokens,
    int PromptTokens,
    DateTimeOffset ExecutedAt
);

public enum CallType
{
    GPT,
    Embedding
}

public enum UsageType
{
    Conversation,
    Indexing
}

public enum CallModel
{
    Gpt35Turbo = 350,
    Gpt35Turbo_16K = 352,
    Gpt4 = 400,
    Gpt4_32K = 402,
    Gpt4_turbo = 1106,
    AdaTextEmbedding = 002
}

public enum CostType
{
    Embedding,
    Completion,
    Prompt
}