namespace ConversationalSearchPlatform.BackOffice.Services.Models;

public record ConversationResponse(Guid ConversationId, string Answer);

public record ConversationReferencedResult(ConversationResponse Response, List<ConversationReference> References);

public record ConversationReference(int Index, string Url);