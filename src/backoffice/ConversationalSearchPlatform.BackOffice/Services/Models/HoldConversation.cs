namespace ConversationalSearchPlatform.BackOffice.Services.Models;

public record HoldConversation(Guid ConversationId, string TenantId, string Prompt, IDictionary<string, string> ConversationContext, Language Language = Language.English)
{

    public Guid ConversationId { get; private set; } = ConversationId;
    public string TenantId { get; private set; } = TenantId;
    public string Prompt { get; set; } = Prompt;
    public Language Language { get; private set; } = Language;
    public IDictionary<string, string> ConversationContext { get; private set; } = ConversationContext;
}