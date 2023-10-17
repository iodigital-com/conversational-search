namespace ConversationalSearchPlatform.BackOffice.Jobs;

public record WebsitePageIndexingDetails(Guid Id, IndexJobChangeType ChangeType) : IIndexingJobDetails;