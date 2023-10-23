using ConversationalSearchPlatform.BackOffice.Api.Conversation;

namespace ConversationalSearchPlatform.BackOffice.Models.Conversations;

public interface TypedConversationRequest
{
    public ConversationEndpointType Type { get; set; }
}