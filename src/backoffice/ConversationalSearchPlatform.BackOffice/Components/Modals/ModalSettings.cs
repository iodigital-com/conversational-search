using MudBlazor;

namespace ConversationalSearchPlatform.BackOffice.Components.Modals;

public static class ModalSettings
{
    public static DialogOptions Default => new()
    {
        CloseOnEscapeKey = true,
        CloseButton = true,
        NoHeader = false
    };
}