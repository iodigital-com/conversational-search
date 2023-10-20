using System.Text.Json.Serialization;

namespace ConversationalSearchPlatform.BackOffice.Services.Models.Weaviate;

public record WeaviateObject(
    Guid Id,
    [property: JsonPropertyName("class")] string ClassName,
    long CreationTimeUnix,
    long? LastUpdateTimeUnix,
    Dictionary<string, object> Properties,
    Dictionary<string, object> Additional,
    float[] Vector,
    object VectorWeights);