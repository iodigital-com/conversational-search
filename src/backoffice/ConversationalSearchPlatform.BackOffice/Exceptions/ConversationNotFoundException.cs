namespace ConversationalSearchPlatform.BackOffice.Exceptions;

public class ConversationNotFoundException : NotFoundException
{
    public ConversationNotFoundException()
    {
    }

    public ConversationNotFoundException(string? message) : base(message)
    {
    }

    public ConversationNotFoundException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}