namespace ConversationalSearchPlatform.BackOffice.Services.Models.Statistics;

public record StatisticsResult(TenantStatisticsResult TenantStatisticsResult);

public record TenantStatisticsResult(string TenantId );