using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ConversationalSearchPlatform.BackOffice.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddOpenAICalculation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "CompletionTokenCost",
                table: "OpenAiConsumptions",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "PromptTokenCost",
                table: "OpenAiConsumptions",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ThousandUnitsCompletionCost",
                table: "OpenAiConsumptions",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ThousandUnitsPromptCost",
                table: "OpenAiConsumptions",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CompletionTokenCost",
                table: "OpenAiConsumptions");

            migrationBuilder.DropColumn(
                name: "PromptTokenCost",
                table: "OpenAiConsumptions");

            migrationBuilder.DropColumn(
                name: "ThousandUnitsCompletionCost",
                table: "OpenAiConsumptions");

            migrationBuilder.DropColumn(
                name: "ThousandUnitsPromptCost",
                table: "OpenAiConsumptions");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "61581AFC-FC42-41BF-A483-F9863B8E4693",
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "06caaa07-e156-465e-a4c9-6c7af29d91c6", "AQAAAAIAAYagAAAAEILILzYccwk9qw642IHe9iwqURG/ZjE59q+w+N3Lmttl2uGjn6VUBmja/NzWZLAsLw==", "f54f1037-0847-49a2-ab65-c2c68860d682" });

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "68657A77-57AE-409D-A845-5ABAF7C1E633",
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "a9428a68-fd63-4864-b9fa-684717f8475f", "AQAAAAIAAYagAAAAEGANfDhfc6AYqwrWs8jBF73sSqCA+wjCj4f2faHfacTevk23pIj/HLO7mGdykckqAA==", "e7ebe747-d50c-4b6e-9e9d-6ea6da9c50a8" });

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "8D4540D4-D50F-48D0-9508-503883712B1A",
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "971ad7d4-0f4b-4b53-bf30-d65a6728a104", "AQAAAAIAAYagAAAAEDQijTIPlrlPCuBWYHlqU0NskwKiTgy5ESRxMiJGFkVhSyBeKu1sJxbaWRGYGthwGA==", "fcf84c3c-a73f-4c63-a5ee-45e65ad8d5fb" });
        }
    }
}
