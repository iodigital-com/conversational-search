using Microsoft.EntityFrameworkCore;

namespace ConversationalSearchPlatform.BackOffice.Data.Seeding;

public interface IDatabaseSeeder
{
    abstract static void Seed(ModelBuilder modelBuilder);
}