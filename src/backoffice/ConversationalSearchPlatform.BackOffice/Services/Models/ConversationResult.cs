using System.Text.Json.Serialization;

namespace ConversationalSearchPlatform.BackOffice.Services.Models;

public record ConversationResult(Guid ConversationId, string Answer, Language Language);

public record ConversationReferencedResult(ConversationResult Result, List<ConversationReference> References);

public record ConversationReference(int Index, string Url, ConversationReferenceType Type);

[Serializable]
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ConversationReferenceType
{
    Official = 0,
    Community = 1
}