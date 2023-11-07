using ConversationalSearchPlatform.BackOffice.Api.Conversation;

namespace ConversationalSearchPlatform.BackOffice.Models.Conversations;

/// <summary>
/// Request to hold a conversation
/// </summary>
/// 
/// <param name="ConversationId">Id of existing conversation</param>
/// <param name="Prompt">The prompt. Usually just contains the question of the end user.</param>
/// <param name="Context">Extra context related variables</param>
/// <param name="Language">The language the conversation is in</param>
/// <param name="Debug">Whether or not we save the debug information</param>
public record ConversationRequest
(Guid? ConversationId,
    string Prompt,
    IDictionary<string, string> Context,
    bool Debug = false,
    LanguageDto Language = LanguageDto.English
) : TypedConversationRequest
{
    public ConversationEndpointType Type { get; set; } = ConversationEndpointType.HoldConversation;
}