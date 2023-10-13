using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Stores;
using Microsoft.EntityFrameworkCore;

namespace ConversationalSearchPlatform.BackOffice.Tenants;

public class EFCoreFactoryCreatingStore<TEFCoreStoreDbContext, TTenantInfo> : IMultiTenantStore<TTenantInfo>
    where TEFCoreStoreDbContext : EFCoreStoreDbContext<TTenantInfo>
    where TTenantInfo : class, ITenantInfo, new()
{
    internal readonly IDbContextFactory<TEFCoreStoreDbContext> _dbContextFactory;

    public EFCoreFactoryCreatingStore(IDbContextFactory<TEFCoreStoreDbContext> dbContextFactory)
    {
        this._dbContextFactory = dbContextFactory ?? throw new ArgumentNullException(nameof(dbContextFactory));
    }

    public virtual async Task<TTenantInfo?> TryGetAsync(string id)
    {
        using (var db = await _dbContextFactory.CreateDbContextAsync())
        {
            return await db.TenantInfo.AsNoTracking()
                .Where(ti => ti.Id == id)
                .SingleOrDefaultAsync();
        }
    }

    public virtual async Task<IEnumerable<TTenantInfo>> GetAllAsync()
    {
        using (var db = await _dbContextFactory.CreateDbContextAsync())
        {
            return await db.TenantInfo.AsNoTracking().ToListAsync();
        }
    }

    public virtual async Task<TTenantInfo?> TryGetByIdentifierAsync(string identifier)
    {
        using (var db = await _dbContextFactory.CreateDbContextAsync())
        {
            return await db.TenantInfo.AsNoTracking()
                .Where(ti => ti.Identifier == identifier)
                .SingleOrDefaultAsync();
        }
    }

    public virtual async Task<bool> TryAddAsync(TTenantInfo tenantInfo)
    {
        using (var db = await _dbContextFactory.CreateDbContextAsync())
        {
            await db.TenantInfo.AddAsync(tenantInfo);
            var result = await db.SaveChangesAsync() > 0;
            db.Entry(tenantInfo).State = EntityState.Detached;

            return result;
        }
    }

    public virtual async Task<bool> TryRemoveAsync(string identifier)
    {
        using (var db = await _dbContextFactory.CreateDbContextAsync())
        {
            var existing = await db.TenantInfo
                .Where(ti => ti.Identifier == identifier)
                .SingleOrDefaultAsync();

            if (existing is null)
            {
                return false;
            }

            db.TenantInfo.Remove(existing);
            return await db.SaveChangesAsync() > 0;
        }
    }

    public virtual async Task<bool> TryUpdateAsync(TTenantInfo tenantInfo)
    {
        using (var db = await _dbContextFactory.CreateDbContextAsync())
        {
            db.TenantInfo.Update(tenantInfo);
            var result = await db.SaveChangesAsync() > 0;
            db.Entry(tenantInfo).State = EntityState.Detached;
            return result;
        }
    }
}