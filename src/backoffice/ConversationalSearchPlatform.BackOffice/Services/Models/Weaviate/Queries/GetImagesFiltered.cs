using System.Text.Json;
using System.Text.Json.Serialization;
using ConversationalSearchPlatform.BackOffice.Constants;
using ConversationalSearchPlatform.BackOffice.Data.Entities;
using GraphQL;

namespace ConversationalSearchPlatform.BackOffice.Services.Models.Weaviate.Queries;

public class GetImagesFiltered
{
    public static string Key = IndexingConstants.ImageClass;

    public record ImageQueryParams(string CollectionName, int Limit, string Prompt, List<string> textReferenceIds) : IQueryParams;

    public GraphQLRequest Request<T>(T @params) where T : IQueryParams
    {
        if (@params is not ImageQueryParams queryParams)
            throw new ArgumentNullException(nameof(queryParams));
        var textReferenceIdsAsJsonArray = JsonSerializer.Serialize(queryParams.textReferenceIds);

        return new GraphQLRequest
        {
            Query = $$"""
                      {
                      	Get {
                      		{{queryParams.CollectionName}}(
                      		  limit: {{queryParams.Limit}}
                      		  nearText: { concepts: "{{queryParams.Prompt}}" }
                              where: {
                      			operator: And
                      			operands: [
                      				{ path: ["internalId"], operator: ContainsAny, valueText: {{textReferenceIdsAsJsonArray}} }
                      			]
                               }
                      		) {
                      		    internalId
                      		    fileName
                      			altDescription
                      			url
                      			title
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

        [JsonPropertyName("url")]
        public string Url { get; set; } = default!;

        [JsonPropertyName("fileName")]
        public string FileName { get; set; } = default!;

        [JsonPropertyName("altDescription")]
        public string? AltDescription { get; set; }
    }

}