using ConversationalSearchPlatform.BackOffice.Paging;
using Finbuckle.MultiTenant;

namespace ConversationalSearchPlatform.BackOffice.Tenants;

public interface IPagingEFCoreFactoryCreatingStore<TTenantInfo> where TTenantInfo : class, ITenantInfo, new()
{
    Task<(IEnumerable<TTenantInfo> items, int count)> GetAllPagedAsync(PageOptions pageOptions);
}