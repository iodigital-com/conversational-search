using ConversationalSearchPlatform.BackOffice.Services.Models;
using ConversationalSearchPlatform.BackOffice.Tenants;
using Microsoft.EntityFrameworkCore;

namespace ConversationalSearchPlatform.BackOffice.Data.Seeding;

public class TenantSeeder : IDatabaseSeeder
{
    public static void Seed(ModelBuilder modelBuilder)
    {
        var iodigital = new ApplicationTenantInfo
        {
            Id = "CCFA9314-ABE6-403A-9E21-2B31D95A5258",
            Identifier = "iodigital",
            Name = "iODigital",
            ChatModel = ChatModel.Gpt4_32K,
            AmountOfSearchReferences = 8
        };

        var polestar = new ApplicationTenantInfo
        {
            Id = "D2FA78CE-3185-458E-964F-8FD0052B4330",
            Identifier = "Polestar",
            Name = "Polestar",
            ChatModel = ChatModel.Gpt4_32K,
            AmountOfSearchReferences = 8
        };

        var iodigitalDemo = new ApplicationTenantInfo
        {
            Id = "4903E29F-D633-4A4C-9065-FE3DD8F27E40",
            Identifier = "iodigitalDemo",
            Name = "iODigitalDemo",
            ChatModel = ChatModel.Gpt35Turbo,
            AmountOfSearchReferences = 8
        };

        modelBuilder.Entity<ApplicationTenantInfo>()
            .HasData(
                TenantConstants.DefaultTenant,
                polestar,
                iodigital,
                iodigitalDemo
            );
    }
}