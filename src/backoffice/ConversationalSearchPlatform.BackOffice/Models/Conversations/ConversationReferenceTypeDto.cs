using System.Text.Json.Serialization;

namespace ConversationalSearchPlatform.BackOffice.Models.Conversations;

[Serializable]
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ConversationReferenceTypeDto
{
    Product = 0,
    Site = 1
}