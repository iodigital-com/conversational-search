using ConversationalSearchPlatform.BackOffice.Models.Conversations;

namespace ConversationalSearchPlatform.BackOffice.Models.Indexing;

public record WebsitePageDto(Guid Id, string Name, string Url, DateTime? IndexedAt, ConversationReferenceTypeDto ReferenceType, LanguageDto Language)
{
    public Guid Id { get; set; } = Id;
    public string Name { get; set; } = Name;
    public string Url { get; set; } = Url;
    public DateTime? IndexedAt { get; set; } = IndexedAt;
    public ConversationReferenceTypeDto ReferenceType { get; set; } = ReferenceType;
    public LanguageDto Language { get; set; } = Language;
}