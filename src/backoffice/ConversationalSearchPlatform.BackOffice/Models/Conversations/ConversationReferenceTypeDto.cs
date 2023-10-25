using System.Text.Json.Serialization;

namespace ConversationalSearchPlatform.BackOffice.Models.Conversations;

[Serializable]
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ConversationReferenceTypeDto
{
    Manual = 0,
    Community = 1
}