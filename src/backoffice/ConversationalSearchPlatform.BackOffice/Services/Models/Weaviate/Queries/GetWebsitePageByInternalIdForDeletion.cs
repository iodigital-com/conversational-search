using System.Text.Json.Serialization;
using ConversationalSearchPlatform.BackOffice.Data.Entities;
using GraphQL;

namespace ConversationalSearchPlatform.BackOffice.Services.Models.Weaviate.Queries;

public class GetWebsitePageByInternalIdForDeletion
{
    public static string Key = nameof(WebsitePage);

    public record GetByInternalIdForDeletionQueryParams(string InternalId) : IQueryParams;

    public GraphQLRequest Request<T>(T @params) where T : IQueryParams
    {
        if (@params is not GetByInternalIdForDeletionQueryParams queryParams)
            throw new ArgumentNullException(nameof(queryParams));


        return new GraphQLRequest
        {
            Query = $$"""
                      {
                      	Get {
                      		{{Key}}(
                              where: {
                      			operator: And
                      			operands: [
                      				{ path: ["internalId"], operator: Equal, valueText: "{{queryParams.InternalId}}" }
                      			]
                               }
                      		) {
                      		    internalId
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
    }
}