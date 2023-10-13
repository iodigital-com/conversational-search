using ConversationalSearchPlatform.BackOffice.Api.Extensions;
using ConversationalSearchPlatform.BackOffice.Constants;
using ConversationalSearchPlatform.BackOffice.Tenants;
using Finbuckle.MultiTenant;

namespace ConversationalSearchPlatform.BackOffice.Api;

public class ApiKeyMiddleware
{

    private readonly RequestDelegate _next;

    public ApiKeyMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task Invoke(HttpContext context, IMultiTenantStore<ApplicationTenantInfo> tenantStore)
    {
        if (context.Request.Path.StartsWithSegments("/api"))
        {
            if (context.Request.Headers.ContainsKey(HeaderConstants.TenantHeader))
            {
                var tenantHeader = context.GetTenantHeader();
                var tenant = await tenantStore.TryGetAsync(tenantHeader);

                if (tenant == null)
                {
                    await UnauthorizedResponse(context, "Invalid api key");
                    return;
                }
            }
            else
            {
                await UnauthorizedResponse(context, "Missing api key");
                return;
            }
        }

        await _next.Invoke(context);
    }

    private static async Task UnauthorizedResponse(HttpContext context, string info)
    {
        context.Response.StatusCode = 401;
        await context.Response.WriteAsync(info);
    }
}