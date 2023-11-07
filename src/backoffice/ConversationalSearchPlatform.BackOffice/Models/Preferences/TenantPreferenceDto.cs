using ConversationalSearchPlatform.BackOffice.Models.Tenants;
using ConversationalSearchPlatform.BackOffice.Services.Models;

namespace ConversationalSearchPlatform.BackOffice.Models.Preferences;

public record TenantPreferenceDto
{
    public TenantPreferenceDto()
    {
    }

    public TenantPreferenceDto(string Identifier,
        string Name,
        ChatModel ChatModel,
        string? BasePrompt,
        int AmountOfSearchReferences,
        List<PromptTagDto> promptTags)
    {
        this.Identifier = Identifier;
        this.Name = Name;
        this.ChatModel = ChatModel;
        this.BasePrompt = BasePrompt;
        this.AmountOfSearchReferences = AmountOfSearchReferences;
        PromptTags = promptTags;
    }

    public string Identifier { get; set; } = default!;
    public string Name { get; set; } = default!;
    public ChatModel ChatModel { get; set; } = ChatModel.Gpt4_32K;
    public string? BasePrompt { get; set; }
    public int AmountOfSearchReferences { get; set; }
    public List<PromptTagDto> PromptTags { get; set; } = default!;
}