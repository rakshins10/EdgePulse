using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EdgePulse.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Sprint15b_UiStringTranslations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UiStringTranslations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LocaleCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Key = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Value = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UiStringTranslations", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UiStringTranslations_Key_Locale_Tenant",
                table: "UiStringTranslations",
                columns: new[] { "Key", "LocaleCode", "TenantId" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_UiStringTranslations_Locale_Tenant",
                table: "UiStringTranslations",
                columns: new[] { "LocaleCode", "TenantId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UiStringTranslations");
        }
    }
}
