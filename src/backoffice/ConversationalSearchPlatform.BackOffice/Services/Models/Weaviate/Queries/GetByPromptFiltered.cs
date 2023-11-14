using System.Text.Json;
using System.Text.Json.Serialization;
using ConversationalSearchPlatform.BackOffice.Data.Entities;
using GraphQL;

namespace ConversationalSearchPlatform.BackOffice.Services.Models.Weaviate.Queries;

public class GetByPromptFiltered
{
    public static string Key = nameof(WebsitePage);

    public record WebsitePageQueryParams(string CollectionName, string TenantId, string Language, string ReferenceType, float[] Vector, int Limit) : IQueryParams;

    public static GraphQLRequest Request<T>(T @params) where T : IQueryParams
    {
        if (@params is not WebsitePageQueryParams queryParams)
            throw new ArgumentNullException(nameof(queryParams));

        var vectorAsJsonArray = JsonSerializer.Serialize(queryParams.Vector);

        return new GraphQLRequest
        {
            Query = $$"""
                      {
                      	Get {
                      		{{queryParams.CollectionName}}(
                      		  limit: {{queryParams.Limit}}
                              nearVector: { vector: {{vectorAsJsonArray}} }
                              where: {
                      			operator: And
                      			operands: [
                      				{ path: ["language"], operator: Equal, valueText: "{{queryParams.Language}}" }
                      				{ path: ["referenceType"], operator: Equal, valueText: "{{queryParams.ReferenceType}}" }
                      				{ path: ["tenantId"], operator: Equal, valueText: "{{queryParams.TenantId}}" }
                      			]
                               }
                      		) {
                      		    internalId
                      		    tenantId
                      			text
                      			title
                      			source
                      			language
                      			referenceType
                      			_additional {
                      	            id,
                      	            certainty,
                      	            distance
                                }
                      		}
                      	}
                      }
                      """
        };
    }

    public record WeaviateRecordResponse : WeaviateGraphQLResponseRecord
    {
        public WeaviateRecordResponse(Additional Additional) : base(Additional)
        {
        }

        [JsonPropertyName("internalId")]

        public string InternalId { get; set; } = default!;

        [JsonPropertyName("tenantId")]

        public string TenantId { get; set; } = default!;

        [JsonPropertyName("text")]
        public string Text { get; set; } = default!;

        [JsonPropertyName("source")]
        public string Source { get; set; } = default!;

        [JsonPropertyName("language")]
        public string Language { get; set; } = default!;

        [JsonPropertyName("referenceType")]
        public string ReferenceType { get; set; } = default!;

        [JsonPropertyName("title")]
        public string Title { get; set; } = default!;
    }
}