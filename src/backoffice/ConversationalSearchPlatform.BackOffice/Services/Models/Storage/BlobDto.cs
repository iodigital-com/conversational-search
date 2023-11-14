namespace ConversationalSearchPlatform.BackOffice.Services.Models.Storage;

public record BlobDto
{
    public string? Uri { get; set; }
    public string? Name { get; set; }
    public string? ContentType { get; set; }
    public Stream? Content { get; set; }
}

public record BlobResponseDto
{
    public string? Status { get; set; }
    public bool Error { get; set; }
    public BlobDto Blob { get; set; } = new();
}