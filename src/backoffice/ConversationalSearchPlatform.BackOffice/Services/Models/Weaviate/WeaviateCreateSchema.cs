using System.Text.Json.Serialization;

namespace ConversationalSearchPlatform.BackOffice.Services.Models.Weaviate;

public record WeaviateCreateSchema(
    string Class,
    string Description,
    string Vectorizer,
    List<SchemaProperty> Properties,
    string? VectorIndexType,
    ModuleConfig? ModuleConfig
)
{

    [JsonPropertyName("class")]
    public string Class { get; set; } = Class;

    [JsonPropertyName("description")]
    public string Description { get; set; } = Description;

    [JsonPropertyName("vectorizer")]
    public string Vectorizer { get; set; } = Vectorizer;

    [JsonPropertyName("vectorIndexType")]
    public string? VectorIndexType { get; set; } = VectorIndexType;

    [JsonPropertyName("properties")]
    public List<SchemaProperty> Properties { get; set; } = Properties;

    [JsonPropertyName("moduleConfig")]
    public ModuleConfig? ModuleConfig { get; set; } = ModuleConfig;
}

public record Multi2VecClip
{
    public List<string> ImageFields { get; set; } = default!;
}

public record ModuleConfig
{
    [JsonPropertyName("multi2vec-clip")]
    public Multi2VecClip Multi2VecClip { get; set; } = default!;
}

public record SchemaProperty(List<string> DataType, string Description, string Name)
{

    [JsonPropertyName("dataType")]
    public List<string> DataType { get; set; } = DataType;

    [JsonPropertyName("description")]
    public string Description { get; set; } = Description;

    [JsonPropertyName("name")]
    public string Name { get; set; } = Name;
}