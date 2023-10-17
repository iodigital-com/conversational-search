namespace ConversationalSearchPlatform.BackOffice.Services;

public interface IVectorizationService
{
    Task BulkCreateAsync(List<object> dataObjects);
}