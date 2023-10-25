namespace ConversationalSearchPlatform.BackOffice.Services.Models;

public record HoldConversation(Guid ConversationId,
    string TenantId,
    string UserPrompt,
    IDictionary<string, string> ConversationContext,
    bool Debug,
    Language Language = Language.English)
{

    public Guid ConversationId { get; private set; } = ConversationId;
    public string TenantId { get; private set; } = TenantId;
    public string UserPrompt { get; set; } = UserPrompt;
    public Language Language { get; private set; } = Language;
    public IDictionary<string, string> ConversationContext { get; private set; } = ConversationContext;
    public bool Debug { get; init; } = Debug;
}