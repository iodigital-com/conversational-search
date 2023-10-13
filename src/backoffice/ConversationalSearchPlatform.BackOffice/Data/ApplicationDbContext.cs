using ConversationalSearchPlatform.BackOffice.Constants;
using ConversationalSearchPlatform.BackOffice.Data.Entities;
using ConversationalSearchPlatform.BackOffice.Data.Seeding;
using ConversationalSearchPlatform.BackOffice.Tenants;
using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ConversationalSearchPlatform.BackOffice.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser, IdentityRole, string>, IMultiTenantDbContext
{
    protected ApplicationDbContext()
    {
        TenantInfo = TenantConstants.DefaultTenant;
    }

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
        TenantInfo = TenantConstants.DefaultTenant;
    }


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<WebsitePage>().HasIndex(x => x.Name);
        modelBuilder.Entity<WebsitePage>().HasIndex(x => x.Name);
        modelBuilder.Entity<WebsitePage>().IsMultiTenant();

        UserAndRolesDatabaseSeeder.Seed(modelBuilder);
    }


    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        this.EnforceMultiTenant();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        this.EnforceMultiTenant();
        return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    public DbSet<WebsitePage> WebsitePages { get; set; }
    public DbSet<UserInvite> UserInvites { get; set; }

    public ITenantInfo TenantInfo { get; }
    public TenantMismatchMode TenantMismatchMode { get; } = TenantMismatchMode.Ignore;
    public TenantNotSetMode TenantNotSetMode { get; } = TenantNotSetMode.Overwrite;
}