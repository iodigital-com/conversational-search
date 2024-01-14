using System.Text.Json.Serialization;

namespace ConversationalSearchPlatform.BackOffice.Services.Models.Weaviate;

public record WeaviateCreateObject<T>(
    [property: JsonPropertyName("class")] string ClassName,
    //float[]? Vector,
    T Properties) where T : IWeaviateCreateRecord;