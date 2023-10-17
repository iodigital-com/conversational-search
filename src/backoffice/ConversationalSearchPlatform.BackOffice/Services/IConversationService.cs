using ConversationalSearchPlatform.BackOffice.Services.Models;

namespace ConversationalSearchPlatform.BackOffice.Services;

public interface IConversationService
{
    Task<ConversationId> StartConversationAsync(StartConversation startConversation);

    Task<ConversationReferencedResult> ConverseAsync(HoldConversation holdConversation);
}