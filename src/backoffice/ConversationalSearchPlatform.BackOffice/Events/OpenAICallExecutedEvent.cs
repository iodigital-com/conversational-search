using ConversationalSearchPlatform.BackOffice.Data;
using Microsoft.EntityFrameworkCore;

namespace ConversationalSearchPlatform.BackOffice.Events;

public record OpenAICallExecutedEvent(Guid CorrelationId,
    string TenantId,
    CallType CallType,
    CallModel CallModel,
    UsageType UsageType,
    int CompletionTokens,
    int PromptTokens,
    DateTimeOffset ExecutedAt
)
{

}

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
    AdaTextEmbedding = 002
}

public class OpenAICallExecutedHandler(
    IDbContextFactory<ApplicationDbContext> dbContextFactory,
    ILogger<OpenAICallExecutedHandler> logger
)
{

    public async Task Handle(OpenAICallExecutedEvent @event)
    {
        try
        {
            using (var db = await dbContextFactory.CreateDbContextAsync())
            {
                // TODO later: can we get actual prices of Azure's openAI?
                var record = new OpenAIConsumption(
                    @event.CorrelationId,
                    @event.TenantId,
                    (ExecutionType)@event.UsageType,
                    (ConsumptionModel)@event.CallModel,
                    (ConsumptionType)@event.CallType,
                    @event.CompletionTokens,
                    @event.PromptTokens,
                    @event.ExecutedAt
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
}