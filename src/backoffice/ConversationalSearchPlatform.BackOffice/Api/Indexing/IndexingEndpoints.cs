using ConversationalSearchPlatform.BackOffice.Api.Extensions;
using ConversationalSearchPlatform.BackOffice.Exceptions;
using ConversationalSearchPlatform.BackOffice.Jobs;
using ConversationalSearchPlatform.BackOffice.Services;
using ConversationalSearchPlatform.BackOffice.Tenants;
using Finbuckle.MultiTenant;
using Hangfire;
using Microsoft.AspNetCore.Mvc;

namespace ConversationalSearchPlatform.BackOffice.Api.Indexing;

public static class IndexingEndpoints
{
    private const string IndexingTag = "Indexing";

    public static IEndpointRouteBuilder MapIndexingGroup(this IEndpointRouteBuilder outerGroup)
    {
        var innerGroup = outerGroup.MapGroup(string.Empty)
            .RequireAuthorization(nameof(TenantApiKeyHeaderRequirement))
            .WithTags(IndexingTag);

        innerGroup.MapPost("index/{websitePageId}",
            async (
                HttpContext httpContext,
                [FromServices] IMultiTenantStore<ApplicationTenantInfo> tenantStore,
                [FromServices] IBackgroundJobClient backgroundJobClient,
                Guid websitePageId
            ) => await ExecuteIndexing(httpContext, tenantStore, backgroundJobClient, websitePageId, IndexJobChangeType.CREATE));

        innerGroup.MapPut("index/{websitePageId}",
            async (
                HttpContext httpContext,
                [FromServices] IMultiTenantStore<ApplicationTenantInfo> tenantStore,
                [FromServices] IBackgroundJobClient backgroundJobClient,
                Guid websitePageId
            ) => await ExecuteIndexing(httpContext, tenantStore, backgroundJobClient, websitePageId, IndexJobChangeType.UPDATE));

        innerGroup.MapDelete("index/{websitePageId}",
            async (
                HttpContext httpContext,
                [FromServices] IMultiTenantStore<ApplicationTenantInfo> tenantStore,
                [FromServices] IBackgroundJobClient backgroundJobClient,
                Guid websitePageId
            ) => await ExecuteIndexing(httpContext, tenantStore, backgroundJobClient, websitePageId, IndexJobChangeType.DELETE));

        return innerGroup;
    }

    private static async Task ExecuteIndexing(
        HttpContext httpContext,
        IMultiTenantStore<ApplicationTenantInfo> tenantStore,
        IBackgroundJobClient backgroundJobClient,
        Guid websitePageId,
        IndexJobChangeType type)
    {
        var tenantId = httpContext.GetTenantHeader();
        var tenant = await tenantStore.TryGetAsync(tenantId);

        if (tenant == null)
        {
            ThrowHelper.ThrowTenantNotFoundException(tenantId);
        }

        backgroundJobClient.Enqueue<WebsitePageIndexingJob>(job =>
            job.Execute(tenantId, new WebsitePageIndexingDetails(websitePageId, type))
        );
    }
}