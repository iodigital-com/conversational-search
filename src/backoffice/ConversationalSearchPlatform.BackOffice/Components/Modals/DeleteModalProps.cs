namespace ConversationalSearchPlatform.BackOffice.Components.Modals;

public record DeleteModalProps(
    string ConfirmationMessage,
    Guid EntityId,
    string EntityName,
    string ModalTitle,
    Func<Task>? DeleteFunction = null,
    Func<Task>? RefreshFunction = null
);