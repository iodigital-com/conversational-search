using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ConversationalSearchPlatform.BackOffice.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdateDecimalPrecision : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "ThousandUnitsPromptCost",
                table: "OpenAiConsumptions",
                type: "decimal(18,8)",
                precision: 18,
                scale: 8,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,4)",
                oldPrecision: 18,
                oldScale: 4);

            migrationBuilder.AlterColumn<decimal>(
                name: "ThousandUnitsCompletionCost",
                table: "OpenAiConsumptions",
                type: "decimal(18,8)",
                precision: 18,
                scale: 8,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,4)",
                oldPrecision: 18,
                oldScale: 4);

            migrationBuilder.AlterColumn<decimal>(
                name: "PromptTokenCost",
                table: "OpenAiConsumptions",
                type: "decimal(18,8)",
                precision: 18,
                scale: 8,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,4)",
                oldPrecision: 18,
                oldScale: 4);

            migrationBuilder.AlterColumn<decimal>(
                name: "CompletionTokenCost",
                table: "OpenAiConsumptions",
                type: "decimal(18,8)",
                precision: 18,
                scale: 8,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,4)",
                oldPrecision: 18,
                oldScale: 4);

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
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "8cf44d81-080f-45bb-8276-a95c37f60589", "AQAAAAIAAYagAAAAELJm0O9EhZ41kP/yM1qArQv+wILcJlkkUNgaYlyf7LH1PaSs1CZBdNFVsJgMTh72wg==", "fd0fd16a-ca2a-46b4-b01c-02f9a54cf445" });

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "8D4540D4-D50F-48D0-9508-503883712B1A",
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "22d608f9-adc4-4123-ad38-a79bc8e9eff0", "AQAAAAIAAYagAAAAEJdbFtkbSt1PvGkGkxeneO+0oCUzwToNX+mwZfwB7jqEjqldVNC/WI7Qqz9Iuv0Rlw==", "c48c67ac-c88e-45c8-9399-6ff7b88c4530" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "ThousandUnitsPromptCost",
                table: "OpenAiConsumptions",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,8)",
                oldPrecision: 18,
                oldScale: 8);

            migrationBuilder.AlterColumn<decimal>(
                name: "ThousandUnitsCompletionCost",
                table: "OpenAiConsumptions",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,8)",
                oldPrecision: 18,
                oldScale: 8);

            migrationBuilder.AlterColumn<decimal>(
                name: "PromptTokenCost",
                table: "OpenAiConsumptions",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,8)",
                oldPrecision: 18,
                oldScale: 8);

            migrationBuilder.AlterColumn<decimal>(
                name: "CompletionTokenCost",
                table: "OpenAiConsumptions",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,8)",
                oldPrecision: 18,
                oldScale: 8);

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "61581AFC-FC42-41BF-A483-F9863B8E4693",
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "dc581531-0f10-4d56-a1ee-e1abf7cf999b", "AQAAAAIAAYagAAAAEN3Pkc59Gi5Lrw6DOKgSK+70G6onPsGWwNYQEAsDiJPIh7bYhtGRNpRbffvwxDKXsA==", "4a87005a-dcef-44eb-8c14-82128c542bbf" });

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "68657A77-57AE-409D-A845-5ABAF7C1E633",
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "f7805266-646c-4915-9e79-cfc1775ba15f", "AQAAAAIAAYagAAAAEEhZHOIiqwh8z189ECx7rbb2bUgcBx/E4r/+tX7brnXn4xz8/M39e1axsxeJYhi2eg==", "21735eaa-beac-43df-9831-5c1cc18bcb6c" });

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "8D4540D4-D50F-48D0-9508-503883712B1A",
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "d726508b-d0ec-4e40-8bc4-604cfd2ec307", "AQAAAAIAAYagAAAAEILlLYbDsvHcNWMxV9/QEvTKZTc/ZHL3w1O3UkJ2F4BvpnX491gOxF6vSR/fh2R74g==", "a69be2ae-c422-4bbc-90a5-3fd3e7d97a69" });
        }
    }
}
