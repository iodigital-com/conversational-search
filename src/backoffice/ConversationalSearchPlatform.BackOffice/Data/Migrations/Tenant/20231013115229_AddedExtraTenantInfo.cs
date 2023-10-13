using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ConversationalSearchPlatform.BackOffice.Data.Migrations.Tenant
{
    /// <inheritdoc />
    public partial class AddedExtraTenantInfo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AmountOfSearchReferences",
                table: "TenantInfo",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "BasePrompt",
                table: "TenantInfo",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ChatModel",
                table: "TenantInfo",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.UpdateData(
                table: "TenantInfo",
                keyColumn: "Id",
                keyValue: "270AFA90-DF18-4FB2-AC10-CFD31E79B238",
                columns: new[] { "AmountOfSearchReferences", "BasePrompt", "ChatModel" },
                values: new object[] { 5, null, 0 });

            migrationBuilder.UpdateData(
                table: "TenantInfo",
                keyColumn: "Id",
                keyValue: "CCFA9314-ABE6-403A-9E21-2B31D95A5258",
                columns: new[] { "AmountOfSearchReferences", "BasePrompt", "ChatModel" },
                values: new object[] { 8, null, 402 });

            migrationBuilder.UpdateData(
                table: "TenantInfo",
                keyColumn: "Id",
                keyValue: "D2FA78CE-3185-458E-964F-8FD0052B4330",
                columns: new[] { "AmountOfSearchReferences", "BasePrompt", "ChatModel" },
                values: new object[] { 8, null, 402 });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AmountOfSearchReferences",
                table: "TenantInfo");

            migrationBuilder.DropColumn(
                name: "BasePrompt",
                table: "TenantInfo");

            migrationBuilder.DropColumn(
                name: "ChatModel",
                table: "TenantInfo");
        }
    }
}
