using System.Text.Json.Serialization;

namespace ConversationalSearchPlatform.BackOffice.Data.Entities;

[Serializable]
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ReferenceType
{
    Manual = 0,
    Community = 1
}