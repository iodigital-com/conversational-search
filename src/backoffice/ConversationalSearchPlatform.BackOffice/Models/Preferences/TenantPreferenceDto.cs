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
        int AmountOfSearchReferences)
    {
        this.Identifier = Identifier;
        this.Name = Name;
        this.ChatModel = ChatModel;
        this.BasePrompt = BasePrompt;
        this.AmountOfSearchReferences = AmountOfSearchReferences;
    }

    public string Identifier { get; set; }
    public string Name { get; set; }
    public ChatModel ChatModel { get; set; } = ChatModel.Gpt4_32K;
    public string? BasePrompt { get; set; }
    public int AmountOfSearchReferences { get; set; }
}