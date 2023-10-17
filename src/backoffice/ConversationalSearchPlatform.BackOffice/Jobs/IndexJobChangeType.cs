using System.Text.Json.Serialization;

namespace ConversationalSearchPlatform.BackOffice.Jobs;

[Serializable]
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum IndexJobChangeType
{
    CREATE,
    UPDATE,
    DELETE
}