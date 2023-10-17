namespace ConversationalSearchPlatform.BackOffice.Jobs;

public interface ITenantAwareIndexingJob <in T> where T : IIndexingJobDetails
{
    Task Execute(string tenantId, T details);

}
