namespace ConversationalSearchPlatform.BackOffice.Models.Users;

public class UserInviteDto(Guid id,
    string email,
    DateTimeOffset createdDate,
    DateTimeOffset expirationDate,
    string code,
    string tenantId)
{

    public Guid Id { get; init; } = id;
    public string Email { get; init; } = email;
    public DateTimeOffset CreatedDate { get; init; } = createdDate;
    public DateTimeOffset ExpirationDate { get; init; } = expirationDate;
    public string Code { get; init; } = code;
    public string TenantId { get; init; } = tenantId;
    public string? TenantName { get; set; }
}