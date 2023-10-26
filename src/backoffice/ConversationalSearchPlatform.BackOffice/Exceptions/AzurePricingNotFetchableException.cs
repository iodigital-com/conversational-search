namespace ConversationalSearchPlatform.BackOffice.Exceptions;

public class AzurePricingNotFetchableException : Exception
{
    public AzurePricingNotFetchableException()
    {
    }

    public AzurePricingNotFetchableException(string? message) : base(message)
    {
    }

    public AzurePricingNotFetchableException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}