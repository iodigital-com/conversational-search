using ConversationalSearchPlatform.BackOffice.Services.Models;

namespace ConversationalSearchPlatform.BackOffice.Services;

public interface IConversationService
{
    Task<ConversationId> StartConversationAsync(StartConversation startConversation, CancellationToken cancellationToken = default);

    Task<ConversationReferencedResult> ConverseAsync(HoldConversation holdConversation, CancellationToken cancellationToken = default);
}