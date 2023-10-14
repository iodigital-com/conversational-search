using ConversationalSearchPlatform.BackOffice.Services.Models;

namespace ConversationalSearchPlatform.BackOffice.Models.Tenants;

public record UpdateTenantDto(
    string Id,
    string Identifier,
    string Name,
    ChatModel ChatModel,
    string? BasePrompt,
    int AmountOfSearchReferences
);