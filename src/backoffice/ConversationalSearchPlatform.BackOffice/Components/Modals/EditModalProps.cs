namespace ConversationalSearchPlatform.BackOffice.Components.Modals;

public record EditModalProps<T>(T item,Func<Task>? RefreshFunction = null);