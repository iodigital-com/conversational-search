using Finbuckle.MultiTenant;

namespace ConversationalSearchPlatform.BackOffice.Data.Entities;

[MultiTenant]
public class WebsitePage : IIndexable, IHaveUrl, IMultiTenant
{
    public WebsitePage(Guid id, string name, string url)
    {
        Id = id;
        Name = name;
        IndexableType = IndexableType.WebsitePage;
        Url = url;
    }

    public WebsitePage(string name, string url)
    {
        Name = name;
        IndexableType = IndexableType.WebsitePage;
        Url = url;
    }

    public Guid Id { get; set; }
    public string Name { get; set; }
    public IndexableType IndexableType { get; init; }
    public DateTimeOffset? IndexedAt { get; set; }
    public string Url { get; set; }
    public string TenantId { get; set; }
}