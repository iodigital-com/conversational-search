using System.ComponentModel.DataAnnotations;

namespace ConversationalSearchPlatform.BackOffice.Data.Entities;

public class UserInvite : IMultiTenant
{
    public UserInvite()
    {
    }

    public UserInvite(string email, DateTimeOffset createdDate, DateTimeOffset expirationDate, string code, string tenantId)
    {
        Email = email;
        CreatedDate = createdDate;
        ExpirationDate = expirationDate;
        Code = code;
        TenantId = tenantId;
    }

    public Guid Id { get; set; }

    [EmailAddress]
    public string Email { get; set; } = default!;

    public DateTimeOffset CreatedDate { get; set; }

    public DateTimeOffset ExpirationDate { get; set; }

    public string Code { get; set; } = default!;

    public string TenantId { get; set; }

    public DateTimeOffset? RedeemDate { get; set; }
    
    public DateTimeOffset MailSentDate { get; set; }
}