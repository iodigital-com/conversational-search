namespace ConversationalSearchPlatform.BackOffice.Exceptions;

public class KeywordExtractionFailedException : Exception
{
    public KeywordExtractionFailedException()
    {
    }

    public KeywordExtractionFailedException(string? message) : base(message)
    {
    }

    public KeywordExtractionFailedException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}
