using ConversationalSearchPlatform.BackOffice.Constants;
using ConversationalSearchPlatform.BackOffice.Jobs;
using ConversationalSearchPlatform.BackOffice.Jobs.Models;
using ConversationalSearchPlatform.BackOffice.Services.Models;
using Hangfire;
using OpenAI.Chat;

namespace ConversationalSearchPlatform.BackOffice.Services.Implementations;

public class OpenAIUsageTelemetryService(IBackgroundJobClient backgroundJobClient) : IOpenAIUsageTelemetryService
{
    public void RegisterEmbeddingUsage(
        Guid correlationId,
        string tenantId,
        ChatTokenUsage usage,
        UsageType usageType = UsageType.Conversation,
        String model = "")
    {
        var evt = new OpenAICallExecutedEvent(
            correlationId,
            tenantId,
            CallType.Embedding,
            Enum.Parse<CallModel>(model.ToString()),
            usageType,
            0,
            usage.InputTokenCount,
            DateTimeOffset.UtcNow
        );
        backgroundJobClient.Enqueue<OpenAICallExecutedHandler>(QueueConstants.TelemetryQueue, handler => handler.Handle(evt));
    }

    public void RegisterGPTUsage(
        Guid correlationId,
        string tenantId,
        ChatTokenUsage usage,
        ChatModel model)
    {
        var evt = new OpenAICallExecutedEvent(
            correlationId,
            tenantId,
            CallType.GPT,
            (CallModel)model,
            UsageType.Conversation,
            usage.OutputTokenCount,
            usage.InputTokenCount,
            DateTimeOffset.UtcNow
        );
        backgroundJobClient.Enqueue<OpenAICallExecutedHandler>(QueueConstants.TelemetryQueue, handler => handler.Handle(evt));
    }
}