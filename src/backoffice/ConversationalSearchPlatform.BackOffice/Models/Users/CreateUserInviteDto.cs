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
    public string Email { get; set; } = default!;

    public DateTime ExpirationDate { get; set; } = default!;
    public ApplicationTenantInfo Tenant { get; set; } = default!;

}