using Rystem.OpenAi.Chat;

namespace ConversationalSearchPlatform.BackOffice.Services.Models;

public record HoldConversation(Guid ConversationId,
    string TenantId,
    ChatMessage UserPrompt,
    IDictionary<string, string> ConversationContext,
    bool Debug,
    Language Language = Language.English)
{

    public Guid ConversationId { get; private set; } = ConversationId;
    public string TenantId { get; private set; } = TenantId;
    public ChatMessage UserPrompt { get; set; } = UserPrompt;
    public Language Language { get; private set; } = Language;
    public IDictionary<string, string> ConversationContext { get; private set; } = ConversationContext;
    public bool Debug { get; init; } = Debug;
}