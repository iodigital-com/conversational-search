using System.Text;
using ConversationalSearchPlatform.BackOffice.Data;
using ConversationalSearchPlatform.BackOffice.Data.Entities;
using ConversationalSearchPlatform.BackOffice.Paging;
using ConversationalSearchPlatform.BackOffice.Services.Models;
using Microsoft.EntityFrameworkCore;

namespace ConversationalSearchPlatform.BackOffice.Services;

public class UserInviteService : IUserInviteService
{
    private readonly IDbContextFactory<ApplicationDbContext> _dbContextFactory;
    private readonly ILogger<UserInviteService> _logger;

    public UserInviteService(IDbContextFactory<ApplicationDbContext> dbContextFactory, ILogger<UserInviteService> logger)
    {
        _dbContextFactory = dbContextFactory;
        _logger = logger;
    }

    public async Task<(List<UserInvite> items, int count)> GetAllPagedAsync(PageOptions pageOptions, CancellationToken cancellationToken = default)
    {
        using (var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken))
        {
            var baseQuery = db.UserInvites.AsQueryable()
                .OrderBy(invite => invite.CreatedDate);

            var skip = (pageOptions.Page - 1) * pageOptions.PageSize;
            var take = pageOptions.PageSize;

            var totalCount = await baseQuery.CountAsync(cancellationToken);

            var invites = await baseQuery
                .Skip(skip)
                .Take(take)
                .ToListAsync(cancellationToken);
            return (invites, totalCount);
        }
    }

    public async Task<UserInvite?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        using (var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken))
        {
            return await db.UserInvites.FirstOrDefaultAsync(x => x.Code == code, cancellationToken);
        }
    }

    public async Task<RedeemResult> RedeemInvitationAsync(string code, CancellationToken cancellationToken = default)
    {
        using (var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken))
        {
            var userInvite = await db.UserInvites
                .FirstOrDefaultAsync(x => x.Code == code, cancellationToken);

            if (userInvite == null)
            {
                return new RedeemResult(RedeemResultStatus.NOT_FOUND);
            }

            if (userInvite.RedeemDate != null)
            {
                return new RedeemResult(RedeemResultStatus.ALREADY_REDEEMED);
            }

            if (userInvite.ExpirationDate < DateTimeOffset.UtcNow)
            {
                return new RedeemResult(RedeemResultStatus.OUTSIDE_EXPIRATION_DATE);
            }

            try
            {
                userInvite.RedeemDate = DateTimeOffset.UtcNow;

                db.Update(userInvite);
                await db.SaveChangesAsync(cancellationToken);

                return new RedeemResult(RedeemResultStatus.SUCCESS);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Unable to redeem code {Code} due to an exception", code);
                return new RedeemResult(RedeemResultStatus.FAILURE);
            }
        }
    }

    public async Task CreateAsync(string email, string tenantId, CancellationToken cancellationToken = default)
    {
        using (var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken))
        {
            var code = GenerateCode(email, tenantId);
            var createdDate = DateTimeOffset.UtcNow;
            var expirationDate = DateTimeOffset.UtcNow.AddDays(14);

            var userInvite = new UserInvite(email, createdDate, expirationDate, code, tenantId);
            db.UserInvites.Add(userInvite);
            await db.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task DeleteByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        using (var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken))
        {
            await db.Set<UserInvite>()
                .Where(page => page.Id == id)
                .ExecuteDeleteAsync(cancellationToken);
        }
    }

    private static string GenerateCode(string email, string tenantId)
    {
        var baseStr = $"{Guid.NewGuid()}_{email}_{tenantId}";
        var converted = Convert.ToBase64String(Encoding.ASCII.GetBytes(baseStr));
        var uriSafe = converted.Replace("/", "_").Replace("+", "-");
        return uriSafe;
    }
}