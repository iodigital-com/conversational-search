using ConversationalSearchPlatform.BackOffice.Api.Conversation;

namespace ConversationalSearchPlatform.BackOffice.Models.Conversations;

/// <summary>
/// Request to start a conversation
/// </summary>
/// 
/// <param name="Language">The language the conversation is in</param>
public record StartConversationRequest(LanguageDto Language = LanguageDto.English) : TypedConversationRequest
{
    public ConversationEndpointType Type { get; set; } = ConversationEndpointType.StartConversation;
}