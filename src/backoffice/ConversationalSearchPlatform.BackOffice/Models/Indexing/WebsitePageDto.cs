namespace ConversationalSearchPlatform.BackOffice.Models.Indexing;

public record WebsitePageDto(Guid Id, string Name, string Url, DateTime? IndexedAt)
{
    public Guid Id { get; set; } = Id;
    public string Name { get; set; } = Name;
    public string Url { get; set; } = Url;
    public DateTime? IndexedAt { get; set; } = IndexedAt;
}