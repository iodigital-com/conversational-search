using System.Text.Json.Serialization;

namespace ConversationalSearchPlatform.BackOffice.Api.Conversation;

[Serializable]
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ConversationEndpointType
{
    StartConversation,
    HoldConversation
}