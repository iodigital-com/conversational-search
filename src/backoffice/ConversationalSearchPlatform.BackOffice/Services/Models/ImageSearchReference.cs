namespace ConversationalSearchPlatform.BackOffice.Services.Models;

public class ImageSearchReference
{
    public string InternalId { get; set; } = default!;
    public string Source { get; set; } = default!;
    public string? AltDescription { get; set; }
    public double? Certainty { get; set; }
}