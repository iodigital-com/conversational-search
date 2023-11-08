using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ConversationalSearchPlatform.BackOffice.Data.Migrations.Tenant
{
    /// <inheritdoc />
    public partial class AddDemoTenant : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "TenantInfo",
                columns: new[]
                {
                    "Id", "AmountOfSearchReferences", "BasePrompt", "ChatModel", "ConnectionString", "Identifier", "Name", "PromptTags"
                },
                values: new object[]
                {
                    "4903E29F-D633-4A4C-9065-FE3DD8F27E40", 8, null, 350, null, "iodigitalDemo", "iODigitalDemo", null
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "TenantInfo",
                keyColumn: "Id",
                keyValue: "4903E29F-D633-4A4C-9065-FE3DD8F27E40");
        }
    }
}