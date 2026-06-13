using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace EdgePulse.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Sprint15_SeedLocales : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Locales",
                columns: new[] { "Id", "Code", "CreatedAt", "DeletedAt", "DisplayName", "Flag", "IsDefault", "IsDeleted", "IsEnabled", "NativeName", "SortOrder", "UpdatedAt" },
                values: new object[,]
                {
                    { new Guid("a0000001-0000-0000-0000-000000000001"), "en", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "English", "🇬🇧", true, false, true, "English", 10, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("a0000001-0000-0000-0000-000000000002"), "fi", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Finnish", "🇫🇮", false, false, true, "Suomi", 20, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("a0000001-0000-0000-0000-000000000003"), "sv", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Swedish", "🇸🇪", false, false, true, "Svenska", 30, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Locales",
                keyColumn: "Id",
                keyValue: new Guid("a0000001-0000-0000-0000-000000000001"));

            migrationBuilder.DeleteData(
                table: "Locales",
                keyColumn: "Id",
                keyValue: new Guid("a0000001-0000-0000-0000-000000000002"));

            migrationBuilder.DeleteData(
                table: "Locales",
                keyColumn: "Id",
                keyValue: new Guid("a0000001-0000-0000-0000-000000000003"));
        }
    }
}
