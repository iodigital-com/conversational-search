namespace ConversationalSearchPlatform.BackOffice.Services.Models.Weaviate;

public record ImageWeaviateCreateRecord(
    string FileName,
    string InternalId, 
    string? AltDescription,
    string? NearByText,
    string Url,
    string Title,
    string Image) : IWeaviateCreateRecord;