using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Web;
using ConversationalSearchPlatform.BackOffice.Data.Entities;
using ConversationalSearchPlatform.BackOffice.Data.Migrations;
using GraphQL;

namespace ConversationalSearchPlatform.BackOffice.Services.Models.Weaviate.Queries;

public class GetByPromptFiltered
{
    public static string Key = nameof(WebsitePage);

    public record WebsitePageQueryParams(string CollectionName, string TenantId, string Language, string ReferenceType, string query, float[] Vector, int Limit) : IQueryParams;

    public static GraphQLRequest Request<T>(T @params) where T : IQueryParams
    {
        if (@params is not WebsitePageQueryParams queryParams)
            throw new ArgumentNullException(nameof(queryParams));

        string cleanQuery = HttpUtility.JavaScriptStringEncode(queryParams.query.ReplaceLineEndings(" "));
        var vectorAsJsonArray = JsonSerializer.Serialize(queryParams.Vector);

        var query = $$"""
                      {
                      	Get {
                      		{{queryParams.CollectionName}}(
                      		  limit: {{queryParams.Limit}}
                              hybrid: 
                              { 
                                  query: "{{cleanQuery}}"
                                  vector: {{vectorAsJsonArray}}
                                  fusionType: relativeScoreFusion
                                  alpha: 0.25
                                  properties: ["title^2", "text"]
                              }
                              where: {
                      			operator: And
                      			operands: [
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
                                articlenumber
                                packaging
                      			_additional {
                      	            id,
                      	            certainty,
                      	            distance
                                }
                      		}
                      	}
                      }
                      """;

        return new GraphQLRequest
        {
            Query = query,
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

        [JsonPropertyName("packaging")]

        public string Packaging { get; set; } = default!;

        [JsonPropertyName("articlenumber")]

        public string ArticleNumber { get; set; } = default!;

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