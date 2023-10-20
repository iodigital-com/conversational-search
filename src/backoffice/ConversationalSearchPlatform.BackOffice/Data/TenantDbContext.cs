using ConversationalSearchPlatform.BackOffice.Data.Seeding;
using ConversationalSearchPlatform.BackOffice.Tenants;
using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Stores;
using Microsoft.EntityFrameworkCore;

namespace ConversationalSearchPlatform.BackOffice.Data;

public class TenantDbContext : EFCoreStoreDbContext<ApplicationTenantInfo>
{
    public TenantDbContext(DbContextOptions<TenantDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<ApplicationTenantInfo>(ati => ati
            .Property(p => p.PromptTags).HasConversion<PromptTagConverter>()
        );
        TenantSeeder.Seed(modelBuilder);
    }
}