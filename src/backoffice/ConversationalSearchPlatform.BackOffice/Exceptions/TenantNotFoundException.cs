namespace ConversationalSearchPlatform.BackOffice.Exceptions;

public class TenantNotFoundException : NotFoundException
{

    public TenantNotFoundException()
    {
    }

    public TenantNotFoundException(string? message) : base(message)
    {
    }

    public TenantNotFoundException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}