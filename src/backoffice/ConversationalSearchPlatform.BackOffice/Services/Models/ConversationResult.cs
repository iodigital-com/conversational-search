using System.Text.Json.Serialization;
using ConversationalSearchPlatform.BackOffice.Services.Models.ConversationDebug;

namespace ConversationalSearchPlatform.BackOffice.Services.Models;

public record ConversationResult(Guid ConversationId, string Answer, Language Language);

public record ConversationReferencedResult(ConversationResult Result, List<ConversationReference> References,
    bool EndConversation, DebugInformation? DebugInformation = default)
{
    public DebugInformation? DebugInformation { get; set; } = DebugInformation;
}

public record ConversationReference(int Index, string Url, string Type, string Title);

[Serializable]
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ConversationReferenceType
{
    Product = 0,
    Site = 1
}