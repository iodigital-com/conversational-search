namespace ConversationalSearchPlatform.BackOffice.Data.Entities;

public class WebsitePageImage : IIndexable, IHaveUrl, IReference
{

    public Guid Id { get; set; }
    public string Name { get; set; }
    public IndexableType IndexableType { get; }
    public DateTimeOffset? IndexedAt { get; set; }
    public string Url { get; set; }
    public ReferenceType ReferenceType { get; set; }
}