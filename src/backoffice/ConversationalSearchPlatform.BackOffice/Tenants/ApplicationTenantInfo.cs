using ConversationalSearchPlatform.BackOffice.Services.Models;
using Finbuckle.MultiTenant;

namespace ConversationalSearchPlatform.BackOffice.Tenants;

public class ApplicationTenantInfo : TenantInfo
{
    public ChatModel ChatModel { get; set; }
    public string? BasePrompt { get; set; }
    public int AmountOfSearchReferences { get; set; } = 5;

    public List<PromptTag>? PromptTags { get; set; }

    public string GetBasePromptOrDefault()
    {
        return string.IsNullOrEmpty(BasePrompt)
            ? $"You are an AI assistant that helps people find information about {Name}. You guide people towards the correct website where they can find all the info they need."
            : BasePrompt;
    }
}