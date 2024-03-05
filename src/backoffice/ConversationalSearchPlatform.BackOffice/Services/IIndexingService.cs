using ConversationalSearchPlatform.BackOffice.Data.Entities;
using ConversationalSearchPlatform.BackOffice.Paging;

namespace ConversationalSearchPlatform.BackOffice.Services;

public interface IIndexingService<T> where T : IIndexable
{
    Task<(List<T> items, int totalCount)> GetAllPagedAsync(PageOptions pageOptions, CancellationToken cancellationToken = default);
    Task<T> CreateAsync(T indexable, string tenantId, CancellationToken cancellationToken = default);

    Task DeleteAllAsync(CancellationToken cancellationToken = default);
    Task<List<T>> CreateBulkAsync(List<T> indexables, CancellationToken cancellationToken = default);
    Task<T> UpdateAsync(T indexable, CancellationToken cancellationToken = default);
    Task DeleteByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<T> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
}