namespace ConversationalSearchPlatform.BackOffice.Paging;

public abstract record PagedRequest(int Page, int PageSize) : PageOptions(Page, PageSize);
