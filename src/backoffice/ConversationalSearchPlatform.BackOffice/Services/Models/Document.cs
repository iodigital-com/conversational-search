using System.Text.Json.Serialization;

namespace ConversationalSearchPlatform.BackOffice.Services.Models;

public class Document
{
    [JsonPropertyName("@search.score")]
    public float SearchScore { get; set; }

    [JsonPropertyName("source")]
    public string Source { get; set; }

    [JsonPropertyName("documentText")]
    public string DocumentText { get; set; }
}