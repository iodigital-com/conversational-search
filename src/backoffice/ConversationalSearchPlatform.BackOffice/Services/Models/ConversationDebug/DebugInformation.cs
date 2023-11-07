using System.Text.Json.Serialization;

namespace ConversationalSearchPlatform.BackOffice.Services.Models.ConversationDebug;

public record DebugInformation
{
    public DebugInformation(List<DebugRecord> debugRecords)
    {
        DebugRecords = debugRecords;
    }

    public int CurrentDebugRecordIndex { get; set; }
    [JsonPropertyName("debug")]
    public List<DebugRecord> DebugRecords { get; set; }
}

public record DebugRecord
{
    [JsonPropertyName("executedAt")]
    public DateTimeOffset ExecutedAt { get; set; }

    [JsonPropertyName("fullPrompt")]
    public string FullPrompt { get; set; } = default!;

    [JsonPropertyName("replacedContextVariables")]
    public Dictionary<string, string> ReplacedContextVariables { get; set; } = default!;

    [JsonPropertyName("references")]
    public References References { get; set; } = default!;
}

public record References
{
    [JsonPropertyName("text")]
    public List<TextDebugInfo> Text { get; set; } = default!;

    [JsonPropertyName("image")]
    public List<ImageDebugInfo> Image { get; set; } = default!;
}

interface IDebugInfo
{
    string InternalId { get; set; }
    bool UsedInAnswer { get; set; }
}

public record TextDebugInfo(string InternalId, bool UsedInAnswer, string Source, string Content) : IDebugInfo
{

    [JsonPropertyName("internalId")]
    public string InternalId { get; set; } = InternalId;

    [JsonPropertyName("usedInAnswer")]
    public bool UsedInAnswer { get; set; } = UsedInAnswer;

    [JsonPropertyName("source")]
    public string Source { get; set; } = Source;

    [JsonPropertyName("content")]
    public string Content { get; set; } = Content;
}

public record ImageDebugInfo(string InternalId, bool UsedInAnswer, string Source, string? AltDescription) : IDebugInfo
{

    [JsonPropertyName("internalId")]
    public string InternalId { get; set; } = InternalId;

    [JsonPropertyName("usedInAnswer")]
    public bool UsedInAnswer { get; set; } = UsedInAnswer;

    [JsonPropertyName("source")]
    public string Source { get; set; } = Source;

    [JsonPropertyName("altDescription")]
    public string? AltDescription { get; set; } = AltDescription;
}