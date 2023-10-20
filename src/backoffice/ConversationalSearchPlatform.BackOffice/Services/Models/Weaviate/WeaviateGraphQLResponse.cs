using System.Text.Json.Serialization;

namespace ConversationalSearchPlatform.BackOffice.Services.Models.Weaviate;

public record Data<T>(T Get)
{
    [JsonPropertyName("Get")]
    public T Get { get; set; } = Get;
}

public record WeaviateGraphQLResponseRecord(Additional? Additional)
{
    [JsonPropertyName("_additional")]
    public Additional? Additional { get; set; } = Additional;

}

public record Additional(double? Certainty, double? Distance, Guid? Id)
{
    [JsonPropertyName("certainty")]
    public double? Certainty { get; set; } = Certainty;

    [JsonPropertyName("distance")]
    public double? Distance { get; set; } = Distance;

    [JsonPropertyName("id")]
    public Guid? Id { get; set; } = Id;
}