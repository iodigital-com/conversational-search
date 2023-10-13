using ConversationalSearchPlatform.BackOffice.Data.Entities;
using ConversationalSearchPlatform.BackOffice.Paging;
using ConversationalSearchPlatform.BackOffice.Services.Models;

namespace ConversationalSearchPlatform.BackOffice.Services;

public interface IUserInviteService
{
    Task<(List<UserInvite> items, int count)> GetAllPagedAsync(PageOptions pageOptions, CancellationToken cancellationToken = default);
    Task<UserInvite?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<RedeemResult> RedeemInvitationAsync(string code, CancellationToken cancellationToken = default);
    Task CreateAsync(string email, string tenantId, CancellationToken cancellationToken = default);
    Task DeleteByIdAsync(Guid id, CancellationToken cancellationToken);
}