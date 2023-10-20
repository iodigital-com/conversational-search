using ConversationalSearchPlatform.BackOffice.Services.Models;
using ConversationalSearchPlatform.BackOffice.Services.Models.Weaviate.Queries;
using GraphQL;

namespace ConversationalSearchPlatform.BackOffice.Services;

public interface IVectorizationService
{
    Task<float[]> CreateVectorAsync(string text);

    Task<List<Guid>> BulkCreateAsync<T>(string collectionName, IInsertableCollection<T> insertableCollection) where T : IInsertable;

    Task<List<R>> SearchAsync<T, R>(string key, GraphQLRequest graphQLRequest, CancellationToken cancellationToken = default)
        where T : IQueryParams
        where R : class;

    Task BulkDeleteAsync(string collectionName, List<Guid> idsToDelete);
}