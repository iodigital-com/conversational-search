using ConversationalSearchPlatform.BackOffice.Jobs.Models;
using ConversationalSearchPlatform.BackOffice.Services.Models;
using ConversationalSearchPlatform.BackOffice.Services.Models.Weaviate.Queries;
using GraphQL;

namespace ConversationalSearchPlatform.BackOffice.Services;

public interface IVectorizationService
{
    Task<float[]> CreateVectorAsync(Guid correlationId, string tenantId, UsageType usageType, string content);

    Task<List<Guid>> BulkCreateAsync<T>(string collectionName, Guid correlationId, string tenantId, UsageType usageType, IInsertableCollection<T> insertableCollection) where T : IInsertable;

    Task<List<R>> SearchAsync<T, R>(string key, GraphQLRequest graphQLRequest, CancellationToken cancellationToken = default)
        where T : IQueryParams
        where R : class;

    Task BulkDeleteAsync(string collectionName, List<Guid> idsToDelete);
}