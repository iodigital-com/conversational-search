using ConversationalSearchPlatform.BackOffice.Paging;
using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Stores;
using Microsoft.EntityFrameworkCore;

namespace ConversationalSearchPlatform.BackOffice.Tenants;

public class EFCoreFactoryCreatingStore<TEFCoreStoreDbContext, TTenantInfo> :
    IMultiTenantStore<TTenantInfo>,
    IPagingEFCoreFactoryCreatingStore<TTenantInfo>,
    IMutatingEFCoreFactoryCreatingStore<TTenantInfo>
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

    public async Task<(IEnumerable<TTenantInfo> items, int count)> GetAllPagedAsync(PageOptions pageOptions)
    {
        using (var db = await _dbContextFactory.CreateDbContextAsync())
        {
            var skip = (pageOptions.Page - 1) * pageOptions.PageSize;
            var take = pageOptions.PageSize;

            var baseQuery = db.TenantInfo.AsNoTracking()
                .OrderBy(info => info.Name)
                .Where(info => info.Id != TenantConstants.DefaultTenant.Id)
                .AsQueryable();

            var totalCount = await baseQuery.CountAsync();

            var items = await baseQuery
                .Skip(skip)
                .Take(take)
                .ToListAsync();

            return (items, totalCount);
        }
    }

    public async Task<TTenantInfo> CreateAsync(TTenantInfo tenant)
    {
        using (var db = await _dbContextFactory.CreateDbContextAsync())
        {
            tenant.Id = Guid.NewGuid().ToString();
            db.TenantInfo.Add(tenant);
            await db.SaveChangesAsync();
            return tenant;
        }
    }

    public async Task<TTenantInfo> UpdateAsync(TTenantInfo tenant)
    {
        using (var db = await _dbContextFactory.CreateDbContextAsync())
        {
            db.TenantInfo.Update(tenant);
            await db.SaveChangesAsync();
            return tenant;
        }
    }

    public async Task DeleteAsync(string tenantId)
    {
        using (var db = await _dbContextFactory.CreateDbContextAsync())
        {
            await db.TenantInfo
                .Where(page => page.Id == tenantId)
                .ExecuteDeleteAsync();
        }
    }
}