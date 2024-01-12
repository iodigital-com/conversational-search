namespace ConversationalSearchPlatform.BackOffice.Services.Models;

public class TextSearchReference
{
    public string Content { get; set; } = default!;
    public double? Certainty { get; set; }
    public string Source { get; set; } = default!;
    public ConversationReferenceType Type { get; set; }
    public Language Language { get; set; }
    public string InternalId { get; set; } = default!;
    public string Title { get; set; } = default!;

    public string ArticleNumber { get; set; } = default!;
    public string Packaging { get; set; } = default!;
}