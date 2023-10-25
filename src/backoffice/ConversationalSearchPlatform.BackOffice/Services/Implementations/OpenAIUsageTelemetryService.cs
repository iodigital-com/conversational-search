using ConversationalSearchPlatform.BackOffice.Events;
using ConversationalSearchPlatform.BackOffice.Services.Models;
using Hangfire;
using Rystem.OpenAi;

namespace ConversationalSearchPlatform.BackOffice.Services.Implementations;

public class OpenAIUsageTelemetryService(IBackgroundJobClient backgroundJobClient) : IOpenAIUsageTelemetryService
{
    public void RegisterEmbeddingUsage(
        Guid correlationId,
        string tenantId,
        Usage usage,
        UsageType usageType = UsageType.Conversation,
        EmbeddingModelType model = EmbeddingModelType.AdaTextEmbedding)
    {
        var evt = new OpenAICallExecutedEvent(
            correlationId,
            tenantId,
            CallType.Embedding,
            (CallModel)model,
            usageType,
            0,
            usage.PromptTokens!.Value,
            DateTimeOffset.UtcNow
        );
        backgroundJobClient.Enqueue<OpenAICallExecutedHandler>(handler => handler.Handle(evt));
    }

    public void RegisterGPTUsage(
        Guid correlationId,
        string tenantId,
        CompletionUsage usage,
        ChatModel model)
    {
        var evt = new OpenAICallExecutedEvent(
            correlationId,
            tenantId,
            CallType.GPT,
            (CallModel)model,
            UsageType.Conversation,
            usage.CompletionTokens!.Value,
            usage.PromptTokens!.Value,
            DateTimeOffset.UtcNow
        );
        backgroundJobClient.Enqueue<OpenAICallExecutedHandler>(handler => handler.Handle(evt));
    }
}