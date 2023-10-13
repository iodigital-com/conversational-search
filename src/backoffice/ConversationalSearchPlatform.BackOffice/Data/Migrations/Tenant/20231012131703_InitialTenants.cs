#nullable disable

using Microsoft.EntityFrameworkCore.Migrations;

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ConversationalSearchPlatform.BackOffice.Data.Migrations.Tenant
{
    /// <inheritdoc />
    public partial class InitialTenants : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TenantInfo",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Identifier = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ConnectionString = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantInfo", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "TenantInfo",
                columns: new[]
                {
                    "Id", "ConnectionString", "Identifier", "Name"
                },
                values: new object[,]
                {
                    {
                        "270AFA90-DF18-4FB2-AC10-CFD31E79B238", null, "DEFAULT", "DEFAULT"
                    },
                    {
                        "CCFA9314-ABE6-403A-9E21-2B31D95A5258", null, "iodigital", "iODigital"
                    },
                    {
                        "D2FA78CE-3185-458E-964F-8FD0052B4330", null, "Polestar", "Polestar"
                    }
                });

            migrationBuilder.CreateIndex(
                name: "IX_TenantInfo_Identifier",
                table: "TenantInfo",
                column: "Identifier",
                unique: true,
                filter: "[Identifier] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TenantInfo");
        }
    }
}