using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ConversationalSearchPlatform.BackOffice.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddedLanguage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Language",
                table: "WebsitePages",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "61581AFC-FC42-41BF-A483-F9863B8E4693",
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "101478f1-55f8-4a20-9106-fd5c41728fc3", "AQAAAAIAAYagAAAAEEt9ctexpNdiJ/nYkdN0Sf0ZugAUE6dSvzJUSerOrmvjtrRSH373YiGEABhwZpTdGg==", "c9beb376-fa37-4190-a62c-782c23fab474" });

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "68657A77-57AE-409D-A845-5ABAF7C1E633",
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "e9d77439-5df0-4414-88cd-1190011f19bc", "AQAAAAIAAYagAAAAEHg3A6Q7cnsBHGNge8NhCvm50upAR8OJPHaFZgdnCDZOGDMk9Ow09zVsiaoormryow==", "e702bf14-0107-4d52-a080-e3d6878598fd" });

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "8D4540D4-D50F-48D0-9508-503883712B1A",
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "b1192a00-6ce8-4daa-bba6-10b861afa85f", "AQAAAAIAAYagAAAAEAKnz/9szdXYHMyQ//veMYUdCeSjpLuGmK5du0gL2/RG0YfpXNf+CNRJdrRvv4VmEg==", "ee6bb69c-ceef-481f-b710-4f8a84795100" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Language",
                table: "WebsitePages");

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
    }
}
