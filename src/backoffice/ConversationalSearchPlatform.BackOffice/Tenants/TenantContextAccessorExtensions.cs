using Finbuckle.MultiTenant;

namespace ConversationalSearchPlatform.BackOffice.Tenants;

public static class TenantContextAccessorExtensions
{
    /// <summary>
    /// Initializes the MultiTenantContext in a requestless scenario. Uses the passed in TenantInfo to set the current tenant.
    /// </summary>
    public static void InitializeForJob<T>(this IMultiTenantContextAccessor<T> multiTenantContextAccessor, T tenantInfo) where T : ApplicationTenantInfo, new()
    {
        if (multiTenantContextAccessor.MultiTenantContext == null)
        {
            multiTenantContextAccessor.MultiTenantContext = new MultiTenantContext<T>
            {
                TenantInfo = tenantInfo,
            };
        }
        else
        {
            multiTenantContextAccessor.MultiTenantContext.TenantInfo = tenantInfo;
        }
    }
}