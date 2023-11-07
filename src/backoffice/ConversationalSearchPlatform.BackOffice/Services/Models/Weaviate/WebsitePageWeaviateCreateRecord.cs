namespace ConversationalSearchPlatform.BackOffice.Services.Models.Weaviate;

public record WebsitePageWeaviateCreateRecord(
    string TenantId,
    string InternalId,
    string Title,
    string Text,
    string Source,
    string Language,
    string ReferenceType
) : IWeaviateCreateRecord;