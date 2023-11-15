using ConversationalSearchPlatform.BackOffice.Services.Models;
using Finbuckle.MultiTenant;

namespace ConversationalSearchPlatform.BackOffice.Data.Entities;

[MultiTenant]
public class WebsitePage : IIndexable, IHaveUrl, IMultiTenant, IReference
{
    public WebsitePage(
        Guid id,
        string name,
        string? url,
        ReferenceType referenceType,
        Language language,
        bool isSitemapParent,
        string? sitemapFileReference,
        string? sitemapFileName
    )
    {
        Id = id;
        Name = name;
        IndexableType = IndexableType.WebsitePage;
        Url = url;
        ReferenceType = referenceType;
        Language = language;
        IsSitemapParent = isSitemapParent;
        SitemapFileReference = sitemapFileReference;
        SitemapFileName = sitemapFileName;
    }

    public WebsitePage(
        string name,
        string? url,
        ReferenceType referenceType,
        Language language,
        bool isSitemapParent,
        string? sitemapFileReference,
        string? sitemapFileName
    )
    {
        Name = name;
        IndexableType = IndexableType.WebsitePage;
        Url = url;
        ReferenceType = referenceType;
        Language = language;
        IsSitemapParent = isSitemapParent;
        SitemapFileReference = sitemapFileReference;
        SitemapFileName = sitemapFileName;
    }

    public Guid Id { get; set; }
    public string Name { get; set; }
    public IndexableType IndexableType { get; init; }
    public DateTimeOffset? IndexedAt { get; set; }
    public string? Url { get; set; }

    public bool IsSitemapParent { get; set; }
    public string? SitemapFileReference { get; set; }
    public string? SitemapFileName { get; set; }

    public Guid? ParentId { get; set; }
    public WebsitePage? Parent { get; set; }
    public string TenantId { get; set; } = default!;

    public ReferenceType ReferenceType { get; set; }

    public Language Language { get; set; }

    public bool IsValidSitemapParent() => IsSitemapParent && this is { SitemapFileName: not null, SitemapFileReference: not null };
}