using System.Text.Json.Serialization;

namespace ConversationalSearchPlatform.BackOffice.Paging;

public record PagedResponse<T, TEmbeddedCollection>
    where T : class
    where TEmbeddedCollection : IEmbeddedCollection<T>, new()
{
    /// <summary>
    /// Wraps the the result set.
    /// </summary>
    [JsonPropertyName("_embedded")]
    public TEmbeddedCollection Embedded { get; set; } = default!;

    /// <summary>
    /// Paging information of the result set.
    /// </summary>
    [JsonPropertyName("_page")]
    public Page Page { get; set; } = null!;

    public PagedResponse()
    {
    }

    public PagedResponse(int page, int pageSize, long? totalElements, IReadOnlyCollection<T> data) =>
        Initialize(page, pageSize, totalElements, data);

    public PagedResponse(int page, int pageSize, long? totalElements, IEnumerable<T> data)
    {
        // enumerate only once
        var dataAsList = data.ToList();
        Initialize(page, pageSize, totalElements, dataAsList);
    }

    private void Initialize(int page, int pageSize, long? totalElements, IReadOnlyCollection<T> data)
    {
        Embedded = new TEmbeddedCollection
        {
            ResourceList = data
        };
        Page = new Page
        {
            Number = page, Size = data.Count, TotalElements = totalElements, TotalPages = CalculateTotalPages(pageSize, totalElements)
        };
    }

    private static int? CalculateTotalPages(int pageSize, long? totalElements)
    {
        int? totalPages = null;

        if (pageSize == 0)
        {
            totalPages = 0;
        }
        else if (totalElements != null)
        {
            totalPages = (int)Math.Ceiling((double)totalElements / pageSize);
        }

        return totalPages;
    }
}
