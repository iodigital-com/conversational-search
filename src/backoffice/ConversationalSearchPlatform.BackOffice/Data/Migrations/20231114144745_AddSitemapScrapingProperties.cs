using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ConversationalSearchPlatform.BackOffice.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSitemapScrapingProperties : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Url",
                table: "WebsitePages",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<bool>(
                name: "IsSitemapParent",
                table: "WebsitePages",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "ParentId",
                table: "WebsitePages",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SitemapFileName",
                table: "WebsitePages",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SitemapFileReference",
                table: "WebsitePages",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "01F243C2-C08C-412F-B2C0-EAB2BCEB4C38",
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "34f698c6-4630-4aaf-b733-b676109e2230", "AQAAAAIAAYagAAAAEPtS8pLOe2TTrWRtBP/VsYRCEWyf5Jx7sCdJnJzxP6Ivtw6EYk28ikxdyRRmhEf3Zg==", "59d35ab5-61c9-414e-912d-e812b2f8fee4" });

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "61581AFC-FC42-41BF-A483-F9863B8E4693",
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "aed18559-b9e4-4797-9b9b-a0a1d87b518c", "AQAAAAIAAYagAAAAEMVbxAqRNlKX/mrEqeZkZkcYzIc8QE+DMCnKcTw3akshy7N7w6+YQlZ0nxqCUxDDng==", "ce8b4bb5-acd5-484f-ab07-991e0f050a5d" });

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "68657A77-57AE-409D-A845-5ABAF7C1E633",
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "45c28633-2afa-41a4-a0d7-c80734f10fd6", "AQAAAAIAAYagAAAAEBAdZ4MF0qC+TokeGXpKt7ssgTsUDlCc+MxsPu37URQPSPuUfvpo2T2eqJ4t+jFSuw==", "3fd24e71-41a2-4d4c-b7b8-7c99e07fe66e" });

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "8D4540D4-D50F-48D0-9508-503883712B1A",
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "796bc471-5132-43c7-95f7-8a69325141f0", "AQAAAAIAAYagAAAAEM1CbMbIMqk/SxgbQ1M7FsV8Ombxu+8B2cYzaI8arDdZeshxC5CdiXrIIJO4wIyZRw==", "3e4d3e39-7ab3-4eb4-9717-a4a3460c62a6" });

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "DBC834A9-2561-4381-BADA-15CF89F0F8A8",
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "36aeeb61-f404-4f89-b6be-efe2739f690f", "AQAAAAIAAYagAAAAEG06GZXUu3klozN6yRogxqFvvclyD3Twae9ED7K8hunPIzuUy3eFkL4qUnfF7zW9wA==", "7236bd60-c489-4578-b708-40b3d5024919" });

            migrationBuilder.CreateIndex(
                name: "IX_WebsitePages_ParentId",
                table: "WebsitePages",
                column: "ParentId");

            migrationBuilder.AddForeignKey(
                name: "FK_WebsitePages_WebsitePages_ParentId",
                table: "WebsitePages",
                column: "ParentId",
                principalTable: "WebsitePages",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WebsitePages_WebsitePages_ParentId",
                table: "WebsitePages");

            migrationBuilder.DropIndex(
                name: "IX_WebsitePages_ParentId",
                table: "WebsitePages");

            migrationBuilder.DropColumn(
                name: "IsSitemapParent",
                table: "WebsitePages");

            migrationBuilder.DropColumn(
                name: "ParentId",
                table: "WebsitePages");

            migrationBuilder.DropColumn(
                name: "SitemapFileName",
                table: "WebsitePages");

            migrationBuilder.DropColumn(
                name: "SitemapFileReference",
                table: "WebsitePages");

            migrationBuilder.AlterColumn<string>(
                name: "Url",
                table: "WebsitePages",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "01F243C2-C08C-412F-B2C0-EAB2BCEB4C38",
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "3ca2d8e6-9639-4949-b088-83b64122e94c", "AQAAAAIAAYagAAAAEAjRqySAOMYVnCjL5hxgQeAlGDgewzL/UHG/gwA1Wh1laHTUP06ychj+nmNRhWJynw==", "1f0f48cd-eafc-4015-8a63-a121f0d1217e" });

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
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "86c7b24d-2363-4a5d-8b14-4a600471c8c4", "AQAAAAIAAYagAAAAEJy2g8Pfb6RFYgJd7II5604xSIH1EfiUcFI89t345PZluWQjzp36yS0luh2D0tuGWA==", "8dc5d51a-9ee0-4aff-a883-9a5ee922afd6" });

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "8D4540D4-D50F-48D0-9508-503883712B1A",
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "d52fe9da-90cf-4c56-bbd4-ee9bea366d52", "AQAAAAIAAYagAAAAEOWL1SjsSWdSZLDoKhWLl0VipiuCF1Kx/YIExcsIE0KQMtAblFbTPngs0lT0kIbLUg==", "2f5dd60d-2263-45ea-9f6b-9e54d7802f65" });

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "DBC834A9-2561-4381-BADA-15CF89F0F8A8",
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "8bce2934-bc71-4485-bc43-2809d3ec7882", "AQAAAAIAAYagAAAAEEaUrQcEbb/469WxGneMN8uK6Gnd7t07Xkgwn5m9KEaShHqLBfnRv5QuRNOUHFGDkA==", "5926fa6f-e8bf-424f-b534-16cd46c78a16" });
        }
    }
}
