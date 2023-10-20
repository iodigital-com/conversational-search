namespace ConversationalSearchPlatform.BackOffice.Services.Models;

public class ImageSearchReference
{

    public string InternalId { get; set; }
    public string Source { get; set; }
    public string? AltDescription { get; set; }
    public double? Certainty { get; set; }
}