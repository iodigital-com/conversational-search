namespace ConversationalSearchPlatform.BackOffice.Models.Conversations;

/// <summary>
/// Response of a newly created conversation
/// </summary>
/// <param name="ConversationId">Id of the created conversation</param>
public record StartConversationResponse(Guid ConversationId);