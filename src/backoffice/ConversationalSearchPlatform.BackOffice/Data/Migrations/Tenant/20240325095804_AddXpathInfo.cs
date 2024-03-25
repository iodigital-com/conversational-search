using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ConversationalSearchPlatform.BackOffice.Data.Migrations.Tenant
{
    /// <inheritdoc />
    public partial class AddXpathInfo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "XPathForSite",
                table: "TenantInfo",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "XPathForSite",
                table: "TenantInfo");
        }
    }
}
