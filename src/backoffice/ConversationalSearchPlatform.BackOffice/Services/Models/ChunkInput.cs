using ConversationalSearchPlatform.BackOffice.Data.Entities;

namespace ConversationalSearchPlatform.BackOffice.Services.Models;

public record ChunkInput(string InternalId, string Name, string HtmlContent, Language Language, string Url, ReferenceType ReferenceType, string TenantId);