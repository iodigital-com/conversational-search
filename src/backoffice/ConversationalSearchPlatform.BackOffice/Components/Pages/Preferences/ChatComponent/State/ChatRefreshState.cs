namespace ConversationalSearchPlatform.BackOffice.Components.Pages.Preferences.ChatComponent.State;

public class ChatRefreshState
{
    public bool ChatNeedsRefresh { get; private set; }

    public event Action? OnChange; 

    public void SetNeedsRefresh()
    {
        ChatNeedsRefresh = true;
        NotifyStateChanged();
    }

    public void SetRefreshed()
    {
        ChatNeedsRefresh = false;
    }

    private void NotifyStateChanged() => OnChange?.Invoke();
}