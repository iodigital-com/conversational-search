using ConversationalSearchPlatform.BackOffice.Services.Models;

namespace ConversationalSearchPlatform.BackOffice.Models.Tenants;

public record TenantDto(
    string Id,
    string Identifier,
    string Name,
    ChatModel ChatModel,
    string? BasePrompt,
    int AmountOfSearchReferences,
    List<PromptTagDto> PromptTags)
{
    public string Id { get; set; } = Id;
    public string Identifier { get; set; } = Identifier;
    public string Name { get; set; } = Name;
    public ChatModel ChatModel { get; set; } = ChatModel;
    public string? BasePrompt { get; set; } = BasePrompt;
    public int AmountOfSearchReferences { get; set; } = AmountOfSearchReferences;
    public List<PromptTagDto> PromptTags { get; set; } = PromptTags;
}