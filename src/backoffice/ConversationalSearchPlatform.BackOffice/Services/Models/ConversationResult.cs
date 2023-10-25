using System.Text.Json.Serialization;
using ConversationalSearchPlatform.BackOffice.Services.Models.ConversationDebug;

namespace ConversationalSearchPlatform.BackOffice.Services.Models;

public record ConversationResult(Guid ConversationId, string Answer, Language Language);

public record ConversationReferencedResult(ConversationResult Result, List<ConversationReference> References, DebugInformation? DebugInformation = default)
{
    public DebugInformation? DebugInformation { get; set; } = DebugInformation;
}

public record ConversationReference(int Index, string Url, ConversationReferenceType Type);

[Serializable]
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ConversationReferenceType
{
    Manual = 0,
    Community = 1
}