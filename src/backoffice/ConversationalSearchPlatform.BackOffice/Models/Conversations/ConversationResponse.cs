namespace ConversationalSearchPlatform.BackOffice.Models.Conversations;

/// <summary>
/// Inner response containing the answer. The answer can contain HTML and references.
/// </summary>
/// <param name="ConversationId">Id of the conversation</param>
/// <param name="Answer">The answer</param>
/// <param name="Language">The language the conversation is in</param>
public record ConversationResponse(Guid ConversationId, string Answer, LanguageDto Language);