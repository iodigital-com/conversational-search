namespace ConversationalSearchPlatform.BackOffice.Services.Models;

public class TextSearchReference
{
    public string Content { get; set; }
    public double? Certainty { get; set; }
    public string Source { get; set; }
    public ConversationReferenceType Type { get; set; }
    public Language Language { get; set; }
    public string InternalId { get; set; }
}