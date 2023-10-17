using ConversationalSearchPlatform.BackOffice.Services.Models;
using Finbuckle.MultiTenant;

namespace ConversationalSearchPlatform.BackOffice.Data.Entities;

[MultiTenant]
public class WebsitePage : IIndexable, IHaveUrl, IMultiTenant, IReference
{
    public WebsitePage(Guid id, string name, string url, ReferenceType referenceType, Language language)
    {
        Id = id;
        Name = name;
        IndexableType = IndexableType.WebsitePage;
        Url = url;
        ReferenceType = referenceType;
        Language = language;
    }

    public WebsitePage(string name, string url, ReferenceType referenceType, Language language)
    {
        Name = name;
        IndexableType = IndexableType.WebsitePage;
        Url = url;
        ReferenceType = referenceType;
        Language = language;
    }

    public Guid Id { get; set; }
    public string Name { get; set; }
    public IndexableType IndexableType { get; init; }
    public DateTimeOffset? IndexedAt { get; set; }
    public string Url { get; set; }
    public string TenantId { get; set; }

    public ReferenceType ReferenceType { get; set; }

    public Language Language { get; set; }
}