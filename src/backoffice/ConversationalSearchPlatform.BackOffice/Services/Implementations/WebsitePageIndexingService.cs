using ConversationalSearchPlatform.BackOffice.Constants;
using ConversationalSearchPlatform.BackOffice.Data;
using ConversationalSearchPlatform.BackOffice.Data.Entities;
using ConversationalSearchPlatform.BackOffice.Jobs;
using ConversationalSearchPlatform.BackOffice.Paging;
using Hangfire;
using Microsoft.EntityFrameworkCore;

namespace ConversationalSearchPlatform.BackOffice.Services.Implementations;

public class WebsitePageIndexingService : IIndexingService<WebsitePage>
{
    private readonly IDbContextFactory<ApplicationDbContext> _dbContextFactory;
    private readonly IBackgroundJobClient _backgroundJobClient;

    public WebsitePageIndexingService(IDbContextFactory<ApplicationDbContext> dbContextFactory, IBackgroundJobClient backgroundJobClient)
    {
        _dbContextFactory = dbContextFactory;
        _backgroundJobClient = backgroundJobClient;
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

    public async Task<WebsitePage> CreateAsync(WebsitePage indexable, string tenantId, CancellationToken cancellationToken = default)
    {
        indexable.TenantId = tenantId;
        using (var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken))
        {
            db.Set<WebsitePage>().Add(indexable);
            await db.SaveChangesAsync(cancellationToken);
            _backgroundJobClient.Enqueue<WebsitePageIndexingJob>(
                QueueConstants.IndexingQueue,
                job => job.Execute(indexable.TenantId, new WebsitePageIndexingDetails(indexable.Id, IndexJobChangeType.CREATE))
            );

            return indexable;
        }
    }

    public async Task<List<WebsitePage>> CreateBulkAsync(List<WebsitePage> indexables, CancellationToken cancellationToken = default)
    {
        using (var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken))
        {
            db.Set<WebsitePage>().AddRange(indexables);
            await db.SaveChangesAsync(cancellationToken);

            foreach (var indexable in indexables)
            {
                _backgroundJobClient.Enqueue<WebsitePageIndexingJob>(
                    QueueConstants.IndexingQueue,
                    job => job.Execute(indexable.TenantId, new WebsitePageIndexingDetails(indexable.Id, IndexJobChangeType.CREATE))
                );
            }

            return indexables;
        }
    }

    public async Task<WebsitePage> UpdateAsync(WebsitePage indexable, CancellationToken cancellationToken = default)
    {
        using (var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken))
        {
            db.Set<WebsitePage>().Update(indexable);
            await db.SaveChangesAsync(cancellationToken);
            _backgroundJobClient.Enqueue<WebsitePageIndexingJob>(
                QueueConstants.IndexingQueue,
                job => job.Execute(indexable.TenantId, new WebsitePageIndexingDetails(indexable.Id, IndexJobChangeType.UPDATE))
            );

            return indexable;
        }
    }

    public async Task DeleteByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using (var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken))
        {
            var indexable = await db.Set<WebsitePage>()
                .FirstOrDefaultAsync(page => page.Id == id, cancellationToken: cancellationToken);

            if (indexable != null)
            {
                db.Remove(indexable);
                await db.SaveChangesAsync(cancellationToken);
                _backgroundJobClient.Enqueue<WebsitePageIndexingJob>(
                    QueueConstants.IndexingQueue,
                    job => job.Execute(indexable.TenantId, new WebsitePageIndexingDetails(id, IndexJobChangeType.DELETE))
                );
            }
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

    public async Task DeleteAllAsync(CancellationToken cancellationToken = default)
    {
        using (var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken))
        {
            var indexables = await db.Set<WebsitePage>().ToListAsync();

            foreach (var indexable in indexables)
            {
                await DeleteByIdAsync(indexable.Id, cancellationToken);
            }
        }
    }
}