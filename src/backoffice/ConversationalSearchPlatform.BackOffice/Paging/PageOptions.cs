namespace ConversationalSearchPlatform.BackOffice.Paging;

/// <summary>
/// Configuration for pagination
/// </summary>
public record PageOptions
{
    /// <summary>
    /// Constructor of PageOptions
    /// </summary>
    public PageOptions()
    {
    }

    /// <summary>
    /// Constructor of PageOptions
    /// </summary>
    /// <param name="page"></param>
    /// <param name="pageSize"></param>
    public PageOptions(int page, int pageSize)
    {
        Page = page == default ? PageConstants.DefaultPageNumber : page;
        PageSize = pageSize == default ? PageConstants.DefaultPageSize : pageSize;
    }

    /// <summary>
    /// Which page has te be selected
    /// </summary>
    public int Page { get; set; } = PageConstants.DefaultPageNumber;

    /// <summary>
    /// Size of the page
    /// </summary>
    public int PageSize { get; set; } = PageConstants.DefaultPageSize;


}
