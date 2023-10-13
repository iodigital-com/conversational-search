using ConversationalSearchPlatform.BackOffice.Services.Models;

namespace ConversationalSearchPlatform.BackOffice.Services;

public interface IConversationService
{
    Task<ConversationId> StartConversationAsync(string tenantId, ChatModel model, int amountOfSearchReferences);

    Task<ConversationReferencedResult> ConverseAsync(Guid conversationId, string tenantId, string prompt);
}