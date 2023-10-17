using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ConversationalSearchPlatform.BackOffice.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddReferenceType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ReferenceType",
                table: "WebsitePages",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "61581AFC-FC42-41BF-A483-F9863B8E4693",
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "95c12d44-ca48-4afa-9084-cefc7325066f", "AQAAAAIAAYagAAAAEAOYXqp6Nu0mA7Gr1RYjEsqNPf88liaWv7itxS3JTNuhM+vuWHugGH/5/VtOM9vplQ==", "b565c294-3288-4d4a-be39-7146ab1df50a" });

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "68657A77-57AE-409D-A845-5ABAF7C1E633",
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "a1b77bd7-79a0-463d-ab69-96e05be32026", "AQAAAAIAAYagAAAAEDi8pAScED4eEyjH6dU138wOlNjJ5RD7M4MYFnpnv++32fZeYekH20PPy8vV3OeWQw==", "a81e3075-1bb4-4e8b-afc5-de86f0569b08" });

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "8D4540D4-D50F-48D0-9508-503883712B1A",
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "c32a2f77-31aa-4f4d-b9b7-641b3fe865d3", "AQAAAAIAAYagAAAAENvCYKccVWmIbFSGecPTD8VhC0Xd1bkOEO3RkNIFm41it55a6Fxj+6NmmQrWmZLazA==", "7b65e187-cf09-44c3-ab7a-ad334f0349f7" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ReferenceType",
                table: "WebsitePages");

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
        }
    }
}
