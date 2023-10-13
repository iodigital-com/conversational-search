using Finbuckle.MultiTenant;
using Microsoft.AspNetCore.Identity;

namespace ConversationalSearchPlatform.BackOffice.Data;

[MultiTenant]
public class ApplicationUser : IdentityUser
{
    public string TenantId { get; set; }
}