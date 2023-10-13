using ConversationalSearchPlatform.BackOffice.Constants;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ConversationalSearchPlatform.BackOffice.Data.Seeding;

public class UserAndRolesDatabaseSeeder : IDatabaseSeeder
{
    public static void Seed(ModelBuilder modelBuilder)
    {
        var userRole = new IdentityRole
        {
            Id = "69FD93B6-C1D1-43C1-A2E9-31C02084EEB6",
            Name = RoleConstants.User,
            NormalizedName = RoleConstants.User.ToUpper()
        };
        var adminRole = new IdentityRole
        {
            Id = "E71D0DC1-4121-4E0B-9F71-F90949029688",
            Name = RoleConstants.Administrator,
            NormalizedName = RoleConstants.Administrator.ToUpper()
        };

        modelBuilder.Entity<IdentityRole>().HasData(adminRole, userRole);

        var iODigitalTenant = "CCFA9314-ABE6-403A-9E21-2B31D95A5258";
        var polestarTenant = "D2FA78CE-3185-458E-964F-8FD0052B4330";

        var hasher = new PasswordHasher<ApplicationUser>();

        var user = new ApplicationUser
        {
            Id = "68657A77-57AE-409D-A845-5ABAF7C1E633",
            UserName = "user",
            NormalizedUserName = "USER",
            PasswordHash = hasher.HashPassword(null!, "iamauser2023"),
            TenantId = polestarTenant,
            EmailConfirmed = true,
            Email = "user@test.com",
            NormalizedEmail = "USER@TEST.COM"
        };

        var user2 = new ApplicationUser
        {
            Id = "8D4540D4-D50F-48D0-9508-503883712B1A",
            UserName = "user2",
            NormalizedUserName = "USER2",
            PasswordHash = hasher.HashPassword(null!, "iamauser2023"),
            TenantId = polestarTenant,
            EmailConfirmed = true,
            Email = "user2@test.com",
            NormalizedEmail = "USER2@TEST.COM"
        };
        var admin = new ApplicationUser
        {
            Id = "61581AFC-FC42-41BF-A483-F9863B8E4693",
            UserName = "admin",
            NormalizedUserName = "admin",
            PasswordHash = hasher.HashPassword(null!, "lPt5i9LxdNeE4h*E"),
            TenantId = iODigitalTenant,
            EmailConfirmed = true,
            Email = "admin@test.com",
            NormalizedEmail = "ADMIN@TEST.com"
        };

        modelBuilder.Entity<ApplicationUser>().HasData(
            user,
            user2,
            admin
        );


        modelBuilder.Entity<IdentityUserRole<string>>().HasData(
            new IdentityUserRole<string>
            {
                RoleId = userRole.Id,
                UserId = user.Id,
            },
            new IdentityUserRole<string>
            {
                RoleId = userRole.Id,
                UserId = user2.Id,
            },
            new IdentityUserRole<string>
            {
                RoleId = adminRole.Id,
                UserId = admin.Id,
            }
        );
    }
}