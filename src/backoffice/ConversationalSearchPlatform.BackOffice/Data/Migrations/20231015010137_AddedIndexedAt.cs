using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ConversationalSearchPlatform.BackOffice.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddedIndexedAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "IndexedAt",
                table: "WebsitePages",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "TenantId",
                table: "UserInvites",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "61581AFC-FC42-41BF-A483-F9863B8E4693",
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "dd38d071-6118-4f84-bf07-7d5efcb894b2", "AQAAAAIAAYagAAAAEOq0frL8M49ynSuwhY6LXM7oL9m8pUo7dbgGQFRquk97E+MiyFmLr0Yoh9cD3T/93w==", "532d414b-caef-490d-811f-9fc541f0e2ac" });

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "68657A77-57AE-409D-A845-5ABAF7C1E633",
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "483000bd-1dca-4229-8392-9c6a0ba541da", "AQAAAAIAAYagAAAAEHwKtn0rTE5DywW19bXamR8WOr7qV32TgEnUfS76WhD89B5MtuxeZ+ySol21hgULFA==", "7683d90e-8482-4f47-8de6-77d461f59fb3" });

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "8D4540D4-D50F-48D0-9508-503883712B1A",
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "bfd74da9-9881-4d23-8cec-7981a5c7f66a", "AQAAAAIAAYagAAAAEA/myuyFxXI6s/U4HBy4AIKecXJdSgC7VKnz2fE7tPIsCRysRdWdnrIttWdRc007FQ==", "155f4adc-4b71-447b-b9fb-315239e683cf" });

            migrationBuilder.CreateIndex(
                name: "IX_WebsitePages_TenantId",
                table: "WebsitePages",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_UserInvites_TenantId",
                table: "UserInvites",
                column: "TenantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_WebsitePages_TenantId",
                table: "WebsitePages");

            migrationBuilder.DropIndex(
                name: "IX_UserInvites_TenantId",
                table: "UserInvites");

            migrationBuilder.DropColumn(
                name: "IndexedAt",
                table: "WebsitePages");

            migrationBuilder.AlterColumn<string>(
                name: "TenantId",
                table: "UserInvites",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "61581AFC-FC42-41BF-A483-F9863B8E4693",
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "8bc350f3-5676-4831-88e3-5837a4c1f505", "AQAAAAIAAYagAAAAECu6+C5gkhNtsMxaBt2/xgbP+d0ixszkYszabebwgrsa/fairMOUtd7kxJo6bUedGg==", "bc481cbe-e23c-4564-923a-feb9ba4fcb78" });

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "68657A77-57AE-409D-A845-5ABAF7C1E633",
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "c05170af-f0d0-4c2e-a3f6-439f62e59bf8", "AQAAAAIAAYagAAAAEAj1OSYivdqvtrzmWtD69SIuqParGIcJJBl5WTuQk3RkVWgbDCe8KRLIxvPNpRsX5g==", "6087071f-40ed-42f2-9da8-37921b2fa8b1" });

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "8D4540D4-D50F-48D0-9508-503883712B1A",
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "8cb9a866-eec2-42e8-afc5-c02e54559ac0", "AQAAAAIAAYagAAAAEE4IoXnzVyoKP0CxMlv8IqAkJM3TeThkqlg7l9nIwF7a4Zc0o+FNbEWyOLuaC3IZLg==", "17b87c63-f6d0-42b1-b274-3486a852e573" });
        }
    }
}
