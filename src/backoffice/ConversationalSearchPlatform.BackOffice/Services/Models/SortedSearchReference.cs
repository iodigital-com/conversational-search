namespace ConversationalSearchPlatform.BackOffice.Services.Models;

public record SortedSearchReference
{
    public int Index { get; set; }
    public TextSearchReference TextSearchReference { get; set; } = default!;
}