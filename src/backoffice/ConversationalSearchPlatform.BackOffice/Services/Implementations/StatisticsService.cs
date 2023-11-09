using System.Globalization;
using ConversationalSearchPlatform.BackOffice.Data;
using ConversationalSearchPlatform.BackOffice.Jobs.Models;
using ConversationalSearchPlatform.BackOffice.Services.Models.Statistics;
using Microsoft.EntityFrameworkCore;

namespace ConversationalSearchPlatform.BackOffice.Services.Implementations;

public class StatisticsService(IDbContextFactory<ApplicationDbContext> dbContextFactory) : IStatisticsService
{

    public async Task<List<CostReport>> GetCostReportForTenantAsync(string tenantId, DateTimeOffset? from, DateTimeOffset? to)
    {
        using (var db = await dbContextFactory.CreateDbContextAsync())
        {
            return await BuildCostReportQuery(db, from, to, tenantId);
        }
    }

    public async Task<List<CostReport>> GetCostReportForAllTenantsAsync(DateTimeOffset? from, DateTimeOffset? to)
    {
        using (var db = await dbContextFactory.CreateDbContextAsync())
        {
            return await BuildCostReportQuery(db, from, to, null);
        }
    }

    private static async Task<List<CostReport>> BuildCostReportQuery(
        ApplicationDbContext db,
        DateTimeOffset? from,
        DateTimeOffset? to,
        string? tenantId
    )
    {
        var query = db.OpenAiConsumptions.AsQueryable();

        if (from != null)
        {
            query = query.Where(consumption => consumption.ExecutedAt >= from);
        }

        if (to != null)
        {
            query = query.Where(consumption => consumption.ExecutedAt <= to);
        }

        if (tenantId != null)
        {
            query = query.Where(consumption => consumption.TenantId == tenantId);
        }

        return await query.Select(consumption =>
                new CostReport(
                    (Month)consumption.ExecutedAt.Month,
                    ISOWeek.GetWeekOfYear(consumption.ExecutedAt.DateTime),
                    (CallModel)consumption.CallModel,
                    (CallType)consumption.CallType,
                    (UsageType)consumption.UsageType,
                    consumption.CompletionTokenCost,
                    consumption.PromptTokenCost,
                    consumption.CompletionTokenCost + consumption.PromptTokenCost
                )
            )
            .ToListAsync();
    }
}