using ConversationalSearchPlatform.BackOffice.Jobs.Models;
using ConversationalSearchPlatform.BackOffice.Services.Models;
using OpenAI.Chat;

namespace ConversationalSearchPlatform.BackOffice.Services;

public interface IOpenAIUsageTelemetryService
{
    void RegisterEmbeddingUsage(
        Guid correlationId,
        string tenantId,
        ChatTokenUsage usage,
        UsageType usageType = UsageType.Conversation,
        String model = ""
    );

    void RegisterGPTUsage(
        Guid correlationId,
        string tenantId,
        ChatTokenUsage usage,
        ChatModel model
    );
}