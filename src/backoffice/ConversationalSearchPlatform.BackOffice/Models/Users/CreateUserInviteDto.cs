using System.ComponentModel.DataAnnotations;
using ConversationalSearchPlatform.BackOffice.Tenants;
using Finbuckle.MultiTenant;

namespace ConversationalSearchPlatform.BackOffice.Models.Users;

public record CreateUserInviteDto
{
    public CreateUserInviteDto()
    {
    }

    public CreateUserInviteDto(string Email,
        DateTime ExpirationDate,
        ApplicationTenantInfo Tenant)
    {
        this.Email = Email;
        this.ExpirationDate = ExpirationDate;
        this.Tenant = Tenant;
    }

    [EmailAddress]
    public string Email { get; set; }

    public DateTime ExpirationDate { get; set; }
    public ApplicationTenantInfo Tenant { get; set; }

}