using System.Text.Json.Serialization;

namespace ConversationalSearchPlatform.BackOffice.Services.Models;

public interface IInsertable;

public interface IInsertableCollection<T> where T : IInsertable
{
    public List<T> Items { get; }
};

public record ImageCollection(string InternalId, List<ImageResult> Items) : IInsertableCollection<ImageResult>;

public record ImageResult(string FileName, string ImageBlob, string? AltDescription, string? NearByText, string Url, string InternalId) : IInsertable;

public record ChunkCollection(string TenantId, string InternalId, string Url, string ReferenceType, string Language, List<ChunkResult> Items) : IInsertableCollection<ChunkResult>;

public record ChunkResult : IInsertable
{
    [JsonPropertyName("type")]
    public string ElementType { get; set; } = default!;

    [JsonPropertyName("element_id")]
    public string ElementId { get; set; } = default!;

    [JsonPropertyName("metadata")]
    public ChunkMetadata MetaData { get; set; } = default!;

    [JsonPropertyName("text")]
    public string Text { get; set; } = default!;

    [JsonPropertyName("articlenumber")]
    public string ArticleNumber { get; set; } = default!;


    [JsonPropertyName("packaging")]
    public string Packaging { get; set; } = default!;
}

public record ChunkMetadata
{
    [JsonPropertyName("filename")]
    public string Filename { get; set; } = default!;

    [JsonPropertyName("filetype")]
    public string ChunkFileType { get; set; } = default!;

    [JsonPropertyName("page_number")]
    public long PageNumber { get; set; }

    [JsonPropertyName("links")]
    public List<Link> Links { get; set; } = default!;
}

public record Link
{
    [JsonPropertyName("text")]
    public string Text { get; set; } = default!;

    [JsonPropertyName("url")]
    public string Url { get; set; } = default!;

    [JsonPropertyName("start_index")]
    public long StartIndex { get; set; }
}