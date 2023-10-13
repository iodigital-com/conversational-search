using ConversationalSearchPlatform.BackOffice.Components.Modals;

namespace ConversationalSearchPlatform.BackOffice.Components.Pages.Indexing;

public record CreateWebsitePageModalProps(
    Func<Task>? RefreshFunction = null
) : CreateModalProps(RefreshFunction);