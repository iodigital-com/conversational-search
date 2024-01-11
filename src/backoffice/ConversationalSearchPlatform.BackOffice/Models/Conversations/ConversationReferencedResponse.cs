using System.Text.Json.Serialization;

namespace ConversationalSearchPlatform.BackOffice.Models.Conversations;

/// <summary>
/// Response of a conversation
/// </summary>
/// <param name="Response">Inner response containing the answer</param>
/// <param name="References">References used in the answer</param>
/// <param name="DebugInformation">Optional debug information about the answer given</param>
/// <param name="EndConversation">Whether the conversation is ended, either because of no references or because the maximum length of the conversation has been exceeded</param>
public record ConversationReferencedResponse(ConversationResponse Response, 
    List<ConversationReferenceResponse> References,
    bool EndConversation,
    DebugInformationResponse? DebugInformation = default);

public record DebugInformationResponse
{
    public DebugInformationResponse(List<DebugRecordResponse> debugRecords)
    {
        DebugRecords = debugRecords;
    }

    [JsonPropertyName("debug")]
    public List<DebugRecordResponse> DebugRecords { get; set; }
}

public record DebugRecordResponse
{
    [JsonPropertyName("executedAt")]
    public DateTimeOffset ExecutedAt { get; set; }

    [JsonPropertyName("fullPrompt")]
    public string FullPrompt { get; set; } = default!;

    [JsonPropertyName("replacedContextVariables")]
    public Dictionary<string, string> ReplacedContextVariables { get; set; } = default!;

    [JsonPropertyName("references")]
    public ReferencesResponse References { get; set; } = default!;
}

public record ReferencesResponse
{
    [JsonPropertyName("text")]
    public List<TextDebugInfoResponse> Text { get; set; } = default!;

    [JsonPropertyName("image")]
    public List<ImageDebugInfoResponse> Image { get; set; } = default!;
}

public record TextDebugInfoResponse(string InternalId, bool UsedInAnswer, string Source, string Content)
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

public record ImageDebugInfoResponse(string InternalId, bool UsedInAnswer, string Source, string? AltDescription)
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