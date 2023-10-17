namespace ConversationalSearchPlatform.BackOffice.Services.Models;

public class SearchReference
{
    public string Content { get; set; }
    public decimal Certainty { get; set; }
    public string Source { get; set; }
    public ConversationReferenceType Type { get; set; }
}