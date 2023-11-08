namespace ConversationalSearchPlatform.BackOffice.Models.Conversations;

/// <summary>
/// Contains possible context variables to be used in a conversation
/// </summary>
/// <param name="Variables"></param>
public record ConversationContextResponse(List<string> Variables);