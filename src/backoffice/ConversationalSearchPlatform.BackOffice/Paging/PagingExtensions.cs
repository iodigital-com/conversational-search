namespace ConversationalSearchPlatform.BackOffice.Paging;

public static class PagingExtensions
{
    /// <summary>
    /// Convert data to a paged response
    /// </summary>
    /// <param name="data">IReadOnlyCollection of data objects of type T</param>
    /// <param name="pageOptions">Options for paging</param>
    /// <param name="total">Total result count</param>
    /// <returns>A paged response</returns>
    public static PagedResponse<T, TEmbeddedCollection> ToPagedResponse<T, TEmbeddedCollection>(
        this IReadOnlyCollection<T> data,
        PageOptions pageOptions,
        long? total = null)
        where TEmbeddedCollection : IEmbeddedCollection<T>, new()
        where T : class => new(pageOptions.Page, pageOptions.PageSize, total, data);


    /// <summary>
    /// Convert data to a paged response
    /// </summary>
    /// <param name="data">IEnumerable of data objects of type T</param>
    /// <param name="pageOptions">Options for paging</param>
    /// <param name="total">Total result count</param>
    /// <returns>A paged response</returns>
    public static PagedResponse<T, TEmbeddedCollection> ToPagedResponse<T, TEmbeddedCollection>(
        this IEnumerable<T> data,
        PageOptions pageOptions,
        long? total = null)
        where TEmbeddedCollection : IEmbeddedCollection<T>, new()
        where T : class => new(pageOptions.Page, pageOptions.PageSize, total, data);
}
