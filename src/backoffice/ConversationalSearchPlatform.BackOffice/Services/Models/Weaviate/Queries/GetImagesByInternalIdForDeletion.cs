using ConversationalSearchPlatform.BackOffice.Constants;
using GraphQL;

namespace ConversationalSearchPlatform.BackOffice.Services.Models.Weaviate.Queries;

public class GetImagesByInternalIdForDeletion
{
    public static string Key = IndexingConstants.ImageClass;

    public record GetImagesByInternalIdForDeletionQueryParams(string InternalId) : IQueryParams;

    public static GraphQLRequest Request<T>(T @params) where T : IQueryParams
    {
        if (@params is not GetImagesByInternalIdForDeletionQueryParams queryParams)
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
                      	            certainty
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