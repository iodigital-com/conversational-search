namespace ConversationalSearchPlatform.BackOffice.Paging;

internal static class PagerHelper
{
    internal static string DeterminePagerInfo(int totalPageElements) =>
        totalPageElements == int.MaxValue ? "{first_item}-{last_item}" : "{first_item}-{last_item} of {all_items}";
}
