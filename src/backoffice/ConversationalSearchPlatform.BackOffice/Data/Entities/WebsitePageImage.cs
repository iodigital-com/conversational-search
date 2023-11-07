namespace ConversationalSearchPlatform.BackOffice.Data.Entities;

public class WebsitePageImage : IIndexable, IHaveUrl, IReference
{

    public Guid Id { get; set; }
    public string Name { get; set; } = default!;
    public IndexableType IndexableType { get; } = default!;
    public DateTimeOffset? IndexedAt { get; set; }
    public string Url { get; set; } = default!;
    public ReferenceType ReferenceType { get; set; }
}