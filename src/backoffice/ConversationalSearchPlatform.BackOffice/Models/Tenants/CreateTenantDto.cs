using ConversationalSearchPlatform.BackOffice.Services.Models;

namespace ConversationalSearchPlatform.BackOffice.Models.Tenants;

public record CreateTenantDto
{
    public CreateTenantDto()
    {
    }

    public CreateTenantDto(string Identifier,
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

    public string Identifier { get; set; } = default!;
    public string Name { get; set; } = default!;
    public ChatModel ChatModel { get; set; } = ChatModel.Gpt4_32K;
    public string? BasePrompt { get; set; }
    public int AmountOfSearchReferences { get; set; }

    public void Deconstruct(out string Identifier, out string Name, out ChatModel ChatModel, out string? BasePrompt, out int AmountOfSearchReferences)
    {
        Identifier = this.Identifier;
        Name = this.Name;
        ChatModel = this.ChatModel;
        BasePrompt = this.BasePrompt;
        AmountOfSearchReferences = this.AmountOfSearchReferences;
    }
}