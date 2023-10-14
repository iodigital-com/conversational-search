using Finbuckle.MultiTenant;

namespace ConversationalSearchPlatform.BackOffice.Tenants;

public interface IMutatingEFCoreFactoryCreatingStore<TTenantInfo> where TTenantInfo : class, ITenantInfo, new()
{
    Task<TTenantInfo> CreateAsync(TTenantInfo tenant);
    Task DeleteAsync(string tenantId);
}