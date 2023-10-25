using ConversationalSearchPlatform.BackOffice.Events;
using ConversationalSearchPlatform.BackOffice.Services.Models;
using Rystem.OpenAi;

namespace ConversationalSearchPlatform.BackOffice.Services;

public interface IOpenAIUsageTelemetryService
{
    void RegisterEmbeddingUsage(
        Guid correlationId,
        string tenantId,
        Usage usage,
        UsageType usageType = UsageType.Conversation,
        EmbeddingModelType model = EmbeddingModelType.AdaTextEmbedding
    );

    void RegisterGPTUsage(
        Guid correlationId,
        string tenantId,
        CompletionUsage usage,
        ChatModel model
    );
}