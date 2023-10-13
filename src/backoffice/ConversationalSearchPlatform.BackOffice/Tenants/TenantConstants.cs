using Finbuckle.MultiTenant;

namespace ConversationalSearchPlatform.BackOffice.Tenants;

public static class TenantConstants
{
    public static readonly ApplicationTenantInfo DefaultTenant = new()
    {
        Id = "270AFA90-DF18-4FB2-AC10-CFD31E79B238",
        Identifier = "DEFAULT",
        Name = "DEFAULT"
    };
}