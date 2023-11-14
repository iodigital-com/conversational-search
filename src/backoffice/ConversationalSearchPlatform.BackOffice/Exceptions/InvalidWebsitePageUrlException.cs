namespace ConversationalSearchPlatform.BackOffice.Exceptions;

public class InvalidWebsitePageUrlException : BadHttpRequestException
{
    public InvalidWebsitePageUrlException(string message) : base(message)
    {
    }

    public InvalidWebsitePageUrlException(string message, Exception innerException) : base(message, innerException)
    {
    }
}