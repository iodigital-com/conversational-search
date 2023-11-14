namespace ConversationalSearchPlatform.BackOffice.Exceptions;

public class WebsitePageNotFoundException : NotFoundException
{

    public WebsitePageNotFoundException()
    {
    }

    public WebsitePageNotFoundException(string? message) : base(message)
    {
    }

    public WebsitePageNotFoundException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}