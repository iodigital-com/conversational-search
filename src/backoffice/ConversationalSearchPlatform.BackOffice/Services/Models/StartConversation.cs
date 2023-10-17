namespace ConversationalSearchPlatform.BackOffice.Services.Models;

public record StartConversation(ChatModel Model, int AmountOfSearchReferences, Language Language = Language.English);

public record ConversationId(Guid Value);