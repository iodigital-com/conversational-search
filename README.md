# ConversationalSearchPlatform

## DB Migrations

### ApplicatinDbContext

```
dotnet ef migrations add --project backoffice/ConversationalSearchPlatform.BackOffice/ConversationalSearchPlatform.BackOffice.csproj --startup-project backoffice/ConversationalSearchPlatform.BackOffice/ConversationalSearchPlatform.BackOffice.csproj --context ConversationalSearchPlatform.BackOffice.Data.ApplicationDbContext --configuration Debug MIGRATION_NAME --output-dir Data/Migrations
dotnet ef database update --project backoffice/ConversationalSearchPlatform.BackOffice/ConversationalSearchPlatform.BackOffice.csproj --startup-project backoffice/ConversationalSearchPlatform.BackOffice/ConversationalSearchPlatform.BackOffice.csproj --context ConversationalSearchPlatform.BackOffice.Data.ApplicationDbContext --configuration Debug GENERATED_MIGRATION_NAME
```

### TentantDbContext

```
dotnet ef migrations add --project backoffice/ConversationalSearchPlatform.BackOffice/ConversationalSearchPlatform.BackOffice.csproj --startup-project backoffice/ConversationalSearchPlatform.BackOffice/ConversationalSearchPlatform.BackOffice.csproj --context ConversationalSearchPlatform.BackOffice.Data.TenantDbContext --configuration Debug MIGRATION_NAME --output-dir Data/Migrations/Tenant
dotnet ef database update --project backoffice/ConversationalSearchPlatform.BackOffice/ConversationalSearchPlatform.BackOffice.csproj --startup-project backoffice/ConversationalSearchPlatform.BackOffice/ConversationalSearchPlatform.BackOffice.csproj --context ConversationalSearchPlatform.BackOffice.Data.TenantDbContext --configuration Debug GENERATED_MIGRATION_NAME
```