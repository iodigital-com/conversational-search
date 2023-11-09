namespace ConversationalSearchPlatform.BackOffice.Services.Models.Statistics;

public record GetTenantStatistics(string TenantId, DateTimeOffset From, DateTimeOffset To);