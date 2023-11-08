using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ConversationalSearchPlatform.BackOffice.Data.Migrations
{
    /// <inheritdoc />
    public partial class DemoRoleAndUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "AspNetRoles",
                columns: new[] { "Id", "ConcurrencyStamp", "Name", "NormalizedName" },
                values: new object[] { "0AD168F8-45F8-441C-878A-E14B8F019229", null, "Readonly", "READONLY" });

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "61581AFC-FC42-41BF-A483-F9863B8E4693",
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "a64b53f1-6514-48b2-8530-720476b1e0bc", "AQAAAAIAAYagAAAAEMRkYptS/B/TMaa60alcb/rQyP4zkU13wuoqPPN0DcJ1BKqba2rT61ouA15k6SaWCg==", "f706318a-aa6f-452e-9d53-4631371b5c59" });

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "68657A77-57AE-409D-A845-5ABAF7C1E633",
                columns: new[] { "ConcurrencyStamp", "NormalizedEmail", "NormalizedUserName", "PasswordHash", "SecurityStamp", "UserName" },
                values: new object[] { "86c7b24d-2363-4a5d-8b14-4a600471c8c4", "USER@POLESTAR.COM", "polestaruser", "AQAAAAIAAYagAAAAEJy2g8Pfb6RFYgJd7II5604xSIH1EfiUcFI89t345PZluWQjzp36yS0luh2D0tuGWA==", "8dc5d51a-9ee0-4aff-a883-9a5ee922afd6", "user@polestar.com" });

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "8D4540D4-D50F-48D0-9508-503883712B1A",
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "d52fe9da-90cf-4c56-bbd4-ee9bea366d52", "AQAAAAIAAYagAAAAEOWL1SjsSWdSZLDoKhWLl0VipiuCF1Kx/YIExcsIE0KQMtAblFbTPngs0lT0kIbLUg==", "2f5dd60d-2263-45ea-9f6b-9e54d7802f65" });

            migrationBuilder.InsertData(
                table: "AspNetUsers",
                columns: new[] { "Id", "AccessFailedCount", "ConcurrencyStamp", "Email", "EmailConfirmed", "LockoutEnabled", "LockoutEnd", "NormalizedEmail", "NormalizedUserName", "PasswordHash", "PhoneNumber", "PhoneNumberConfirmed", "SecurityStamp", "TenantId", "TwoFactorEnabled", "UserName" },
                values: new object[,]
                {
                    { "01F243C2-C08C-412F-B2C0-EAB2BCEB4C38", 0, "3ca2d8e6-9639-4949-b088-83b64122e94c", "demoadmin@iodigital.com", true, false, null, "DEMOADMIN@IODIGITAL.COM", "IODIGITALDEMOADMIN", "AQAAAAIAAYagAAAAEAjRqySAOMYVnCjL5hxgQeAlGDgewzL/UHG/gwA1Wh1laHTUP06ychj+nmNRhWJynw==", null, false, "1f0f48cd-eafc-4015-8a63-a121f0d1217e", "4903E29F-D633-4A4C-9065-FE3DD8F27E40", false, "iodigitalDemoAdmin" },
                    { "DBC834A9-2561-4381-BADA-15CF89F0F8A8", 0, "8bce2934-bc71-4485-bc43-2809d3ec7882", "demo@iodigital.com", true, false, null, "DEMO@IODIGITAL.COM", "IODIGITALDEMO", "AQAAAAIAAYagAAAAEEaUrQcEbb/469WxGneMN8uK6Gnd7t07Xkgwn5m9KEaShHqLBfnRv5QuRNOUHFGDkA==", null, false, "5926fa6f-e8bf-424f-b534-16cd46c78a16", "4903E29F-D633-4A4C-9065-FE3DD8F27E40", false, "iodigitalDemo" }
                });

            migrationBuilder.InsertData(
                table: "AspNetUserRoles",
                columns: new[] { "RoleId", "UserId" },
                values: new object[,]
                {
                    { "E71D0DC1-4121-4E0B-9F71-F90949029688", "01F243C2-C08C-412F-B2C0-EAB2BCEB4C38" },
                    { "0AD168F8-45F8-441C-878A-E14B8F019229", "DBC834A9-2561-4381-BADA-15CF89F0F8A8" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "AspNetUserRoles",
                keyColumns: new[] { "RoleId", "UserId" },
                keyValues: new object[] { "E71D0DC1-4121-4E0B-9F71-F90949029688", "01F243C2-C08C-412F-B2C0-EAB2BCEB4C38" });

            migrationBuilder.DeleteData(
                table: "AspNetUserRoles",
                keyColumns: new[] { "RoleId", "UserId" },
                keyValues: new object[] { "0AD168F8-45F8-441C-878A-E14B8F019229", "DBC834A9-2561-4381-BADA-15CF89F0F8A8" });

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "0AD168F8-45F8-441C-878A-E14B8F019229");

            migrationBuilder.DeleteData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "01F243C2-C08C-412F-B2C0-EAB2BCEB4C38");

            migrationBuilder.DeleteData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "DBC834A9-2561-4381-BADA-15CF89F0F8A8");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "61581AFC-FC42-41BF-A483-F9863B8E4693",
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "1128472d-a18d-451f-bb74-5159673acab2", "AQAAAAIAAYagAAAAEOdQs4uHIaSylSaWPCXsvsP7ANS2ZwKQaRPnspgkRUavUjNiWdmAqt8Qgozny0nooA==", "ecacce01-9a91-4792-8756-dc859fb06eaa" });

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "68657A77-57AE-409D-A845-5ABAF7C1E633",
                columns: new[] { "ConcurrencyStamp", "NormalizedEmail", "NormalizedUserName", "PasswordHash", "SecurityStamp", "UserName" },
                values: new object[] { "8cf44d81-080f-45bb-8276-a95c37f60589", "USER@TEST.COM", "USER", "AQAAAAIAAYagAAAAELJm0O9EhZ41kP/yM1qArQv+wILcJlkkUNgaYlyf7LH1PaSs1CZBdNFVsJgMTh72wg==", "fd0fd16a-ca2a-46b4-b01c-02f9a54cf445", "user" });

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "8D4540D4-D50F-48D0-9508-503883712B1A",
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "22d608f9-adc4-4123-ad38-a79bc8e9eff0", "AQAAAAIAAYagAAAAEJdbFtkbSt1PvGkGkxeneO+0oCUzwToNX+mwZfwB7jqEjqldVNC/WI7Qqz9Iuv0Rlw==", "c48c67ac-c88e-45c8-9399-6ff7b88c4530" });
        }
    }
}
