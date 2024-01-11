using System.Text.Json.Serialization;

namespace ConversationalSearchPlatform.BackOffice.Data.Entities;

[Serializable]
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ReferenceType
{
    Product = 0,
    Site = 1
}