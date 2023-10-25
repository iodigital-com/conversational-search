using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ConversationalSearchPlatform.BackOffice.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddOpenAIConsumption : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OpenAiConsumptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CorrelationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CallType = table.Column<int>(type: "int", nullable: false),
                    CallModel = table.Column<int>(type: "int", nullable: false),
                    UsageType = table.Column<int>(type: "int", nullable: false),
                    CompletionTokens = table.Column<int>(type: "int", nullable: false),
                    PromptTokens = table.Column<int>(type: "int", nullable: false),
                    ExecutedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OpenAiConsumptions", x => x.Id);
                });

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

            migrationBuilder.CreateIndex(
                name: "IX_OpenAiConsumptions_CorrelationId",
                table: "OpenAiConsumptions",
                column: "CorrelationId");

            migrationBuilder.CreateIndex(
                name: "IX_OpenAiConsumptions_ExecutedAt",
                table: "OpenAiConsumptions",
                column: "ExecutedAt");

            migrationBuilder.CreateIndex(
                name: "IX_OpenAiConsumptions_TenantId",
                table: "OpenAiConsumptions",
                column: "TenantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OpenAiConsumptions");

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
    }
}
