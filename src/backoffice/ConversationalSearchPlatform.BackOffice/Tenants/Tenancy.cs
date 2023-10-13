using System.Diagnostics.CodeAnalysis;
using System.Security.Claims;
using ConversationalSearchPlatform.BackOffice.Caching;
using ConversationalSearchPlatform.BackOffice.Constants;
using ConversationalSearchPlatform.BackOffice.Data;
using Finbuckle.MultiTenant;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.IdentityModel.Tokens;

namespace ConversationalSearchPlatform.BackOffice.Tenants;

public static class Tenancy
{
    private readonly static DistributedCacheEntryOptions CacheEntryOptions = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1)
    };

    public static async Task<string?> ByUserStrategy(object state)
    {
        if (state is not HttpContext httpContext)
            return null;

        var tenantContext = httpContext.GetMultiTenantContext<ApplicationTenantInfo>();
        var tenantStore = httpContext.RequestServices.GetRequiredService<IMultiTenantStore<ApplicationTenantInfo>>();
        var userManager = httpContext.RequestServices.GetRequiredService<UserManager<ApplicationUser>>();
        var cache = httpContext.RequestServices.GetRequiredService<IDistributedCache>();

        //Someone already set the tenant. Likely another strategy  
        if (tenantContext is not null && tenantContext.HasResolvedTenant)
            return tenantContext.TenantInfo!.Identifier;

        var principal = httpContext.User;

        var applicationUser = await GetApplicationUserCachedAsync(cache, userManager, principal);

        if (applicationUser == null)
        {
            return TenantConstants.DefaultTenant.Identifier;
        }

        var tenant = await GetTenantCachedAsync(cache, tenantStore, applicationUser.TenantId);

        return tenant != null ? tenant.Identifier : TenantConstants.DefaultTenant.Identifier;
    }

    private static async Task<ApplicationUser?> GetApplicationUserCachedAsync(IDistributedCache cache, UserManager<ApplicationUser> userManager, ClaimsPrincipal principal)
    {
        var cacheKey = $"user_{principal.Identity!.Name!}";

        var applicationUser = await cache.GetAsync<ApplicationUser>(cacheKey);

        if (applicationUser != null)
        {
            return applicationUser;
        }

        applicationUser = await userManager.GetUserAsync(principal);

        if (applicationUser != null)
        {
            await cache.SetAsync(
                cacheKey,
                applicationUser,
                CacheEntryOptions
            );
        }

        return applicationUser;
    }

    private static async Task<ApplicationTenantInfo?> GetTenantCachedAsync(IDistributedCache cache, IMultiTenantStore<ApplicationTenantInfo> tenantStore, string tenantId)
    {
        var cacheKey = $"tenant_{tenantId}";

        var tenant = await cache.GetAsync<ApplicationTenantInfo>(cacheKey);

        if (tenant != null)
        {
            return tenant;
        }

        tenant = await tenantStore.TryGetAsync(tenantId);

        if (tenant != null)
        {
            await cache.SetAsync(
                cacheKey,
                tenant,
                CacheEntryOptions
            );
        }

        return tenant;
    }
}