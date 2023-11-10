using ConversationalSearchPlatform.BackOffice.Services.Models.Statistics;

namespace ConversationalSearchPlatform.BackOffice.Services;

public interface IStatisticsService
{
    Task<List<CostReport>> GetCostReportForTenantAsync(string tenantId, DateTimeOffset? from, DateTimeOffset? to);
    Task<List<CostReport>> GetCostReportForAllTenantsAsync(DateTimeOffset? from, DateTimeOffset? to);
    Task<List<ConversationCostReport>> GetConversationCostReportForTenantAsync(string tenantId, DateTimeOffset? from, DateTimeOffset? to);
    Task<List<ConversationCostReport>> GetConversationCostReportForAllTenantsAsync(DateTimeOffset? from, DateTimeOffset? to);

}