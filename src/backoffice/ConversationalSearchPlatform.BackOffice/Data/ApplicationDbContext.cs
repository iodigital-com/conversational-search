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
    private readonly IMultiTenantContextAccessor<ApplicationTenantInfo> _multiTenantContextAccessor;

    protected ApplicationDbContext(IMultiTenantContextAccessor<ApplicationTenantInfo> multiTenantContextAccessor)
    {
        _multiTenantContextAccessor = multiTenantContextAccessor;
        TenantInfo = _multiTenantContextAccessor.MultiTenantContext?.TenantInfo ?? TenantConstants.DefaultTenant;
    }

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, IMultiTenantContextAccessor<ApplicationTenantInfo> multiTenantContextAccessor) : base(options)
    {
        _multiTenantContextAccessor = multiTenantContextAccessor;
        TenantInfo = _multiTenantContextAccessor.MultiTenantContext?.TenantInfo ?? TenantConstants.DefaultTenant;
    }


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<WebsitePage>().HasIndex(x => x.Name);
        modelBuilder.Entity<WebsitePage>().HasIndex(x => x.Name);
        modelBuilder.Entity<WebsitePage>().IsMultiTenant();
        modelBuilder.Entity<WebsitePage>().HasIndex(w => w.TenantId);

        modelBuilder.Entity<UserInvite>().HasIndex(w => w.TenantId);

        modelBuilder.Entity<OpenAIConsumption>().HasIndex(oc => oc.TenantId);
        modelBuilder.Entity<OpenAIConsumption>().HasIndex(oc => oc.ExecutedAt);
        modelBuilder.Entity<OpenAIConsumption>().HasIndex(oc => oc.CorrelationId);
        modelBuilder.Entity<OpenAIConsumption>().Property(oc => oc.CompletionTokenCost).HasPrecision(18, 8);
        modelBuilder.Entity<OpenAIConsumption>().Property(oc => oc.PromptTokenCost).HasPrecision(18, 8);
        modelBuilder.Entity<OpenAIConsumption>().Property(oc => oc.ThousandUnitsCompletionCost).HasPrecision(18, 8);
        modelBuilder.Entity<OpenAIConsumption>().Property(oc => oc.ThousandUnitsPromptCost).HasPrecision(18, 8);

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

    public DbSet<OpenAIConsumption> OpenAiConsumptions { get; set; }

    public ITenantInfo TenantInfo { get; set; }
    public TenantMismatchMode TenantMismatchMode { get; } = TenantMismatchMode.Ignore;
    public TenantNotSetMode TenantNotSetMode { get; } = TenantNotSetMode.Overwrite;
}