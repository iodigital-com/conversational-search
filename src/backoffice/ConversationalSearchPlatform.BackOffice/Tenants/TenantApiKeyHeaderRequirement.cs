using System.Globalization;
using ConversationalSearchPlatform.BackOffice.Api.Extensions;
using ConversationalSearchPlatform.BackOffice.Constants;
using Finbuckle.MultiTenant;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Newtonsoft.Json;

namespace ConversationalSearchPlatform.BackOffice.Tenants;

public class TenantApiKeyHeaderRequirement : IAuthorizationRequirement;

public class TenantApiKeyHeaderHandler : AuthorizationHandler<TenantApiKeyHeaderRequirement>
{

    private readonly IHttpContextAccessor _contextAccessor;
    private readonly IMultiTenantStore<ApplicationTenantInfo> _tenantStore;

    public TenantApiKeyHeaderHandler(IHttpContextAccessor contextAccessor, IMultiTenantStore<ApplicationTenantInfo> tenantStore)
    {
        _contextAccessor = contextAccessor;
        _tenantStore = tenantStore;
    }


    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, TenantApiKeyHeaderRequirement requirement)
    {
        var httpContext = _contextAccessor.HttpContext!;

        if (httpContext.Request.Headers.ContainsKey(HeaderConstants.TenantHeader))
        {
            var tenantHeader = httpContext.GetTenantHeader();
            var tenant = await _tenantStore.TryGetAsync(tenantHeader);

            if (tenant == null)
            {
                context.Fail();
            }

            context.Succeed(requirement);
        }
        else
        {
            context.Fail();
        }
    }
}