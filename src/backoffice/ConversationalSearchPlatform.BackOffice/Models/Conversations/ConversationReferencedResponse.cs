namespace ConversationalSearchPlatform.BackOffice.Models.Conversations;

/// <summary>
/// Response of a conversation
/// </summary>
/// <param name="Response">Inner response containing the answer</param>
/// <param name="References"></param>
public record ConversationReferencedResponse(ConversationResponse Response, List<ConversationReferenceResponse> References);