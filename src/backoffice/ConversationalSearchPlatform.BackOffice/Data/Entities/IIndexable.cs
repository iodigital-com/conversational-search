namespace ConversationalSearchPlatform.BackOffice.Data.Entities;

public interface IIndexable
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public IndexableType IndexableType { get; }
    public DateTimeOffset? IndexedAt { set; get; }
}