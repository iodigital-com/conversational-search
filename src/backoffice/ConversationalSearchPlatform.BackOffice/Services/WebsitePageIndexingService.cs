using ConversationalSearchPlatform.BackOffice.Data;
using ConversationalSearchPlatform.BackOffice.Data.Entities;
using ConversationalSearchPlatform.BackOffice.Paging;
using Microsoft.EntityFrameworkCore;

namespace ConversationalSearchPlatform.BackOffice.Services;

public class WebsitePageIndexingService : IIndexingService<WebsitePage>
{
    private readonly IDbContextFactory<ApplicationDbContext> _dbContextFactory;

    public WebsitePageIndexingService(IDbContextFactory<ApplicationDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task<(List<WebsitePage> items, int totalCount)> GetAllPagedAsync(PageOptions pageOptions, CancellationToken cancellationToken = default)
    {
        using (var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken))
        {
            var skip = (pageOptions.Page - 1) * pageOptions.PageSize;
            var take = pageOptions.PageSize;

            var baseQuery = db.Set<WebsitePage>().AsQueryable()
                .OrderBy(page => page.Name);
            var totalCount = await baseQuery.CountAsync(cancellationToken);

            var pages = await baseQuery
                .Skip(skip)
                .Take(take)
                .ToListAsync(cancellationToken);
            return (pages, totalCount);
        }
    }

    public async Task<WebsitePage> CreateAsync(WebsitePage indexable, CancellationToken cancellationToken = default)
    {
        using (var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken))
        {
            db.Set<WebsitePage>().Add(indexable);
            await db.SaveChangesAsync(cancellationToken);
            //TODO execute some kind of job here to actually index the indexed version

            return indexable;
        }
    }

    public async Task<WebsitePage> UpdateAsync(WebsitePage indexable, CancellationToken cancellationToken = default)
    {
        using (var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken))
        {
            db.Set<WebsitePage>().Update(indexable);
            await db.SaveChangesAsync(cancellationToken);
            //TODO execute some kind of job here to actually update the indexed version

            return indexable;
        }
    }

    public async Task DeleteByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using (var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken))
        {
            await db.Set<WebsitePage>()
                .Where(page => page.Id == id)
                .ExecuteDeleteAsync(cancellationToken);
            //TODO execute some kind of job here to delete the indexed version
        }
    }

    public async Task<WebsitePage> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using (var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken))
        {
            return await db.Set<WebsitePage>()
                .SingleAsync(page => page.Id == id, cancellationToken);
        }
    }
}