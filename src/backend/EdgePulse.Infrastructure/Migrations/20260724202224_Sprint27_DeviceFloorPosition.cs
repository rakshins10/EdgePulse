using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EdgePulse.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Sprint27_DeviceFloorPosition : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "FloorX",
                table: "Devices",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "FloorY",
                table: "Devices",
                type: "float",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FloorX",
                table: "Devices");

            migrationBuilder.DropColumn(
                name: "FloorY",
                table: "Devices");
        }
    }
}
