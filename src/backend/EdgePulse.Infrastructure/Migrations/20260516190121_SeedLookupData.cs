using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace EdgePulse.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SeedLookupData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "MetricTypes",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "MetricTypes",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "DefaultUnit",
                table: "MetricTypes",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Code",
                table: "MetricTypes",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "DeviceStatuses",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "DeviceStatuses",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Color",
                table: "DeviceStatuses",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Code",
                table: "DeviceStatuses",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "AlertStatuses",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "AlertStatuses",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Code",
                table: "AlertStatuses",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "AlertSeverities",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "AlertSeverities",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Color",
                table: "AlertSeverities",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Code",
                table: "AlertSeverities",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.InsertData(
                table: "AlertSeverities",
                columns: new[] { "Id", "Code", "Color", "CreatedAt", "DeletedAt", "Description", "IndustryTemplateId", "IsActive", "IsDeleted", "IsSystem", "Name", "Priority", "SortOrder", "TemplateId", "TenantId", "UpdatedAt" },
                values: new object[,]
                {
                    { new Guid("00000033-0000-0000-0000-000000000001"), "CRITICAL", "#ef4444", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Immediate action required -- machine must stop", null, true, false, true, "Critical", 1, 1, new Guid("00000010-0000-0000-0000-000000000003"), null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("00000033-0000-0000-0000-000000000002"), "HIGH", "#f97316", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Action required within 1 hour", null, true, false, true, "High", 2, 2, new Guid("00000010-0000-0000-0000-000000000003"), null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("00000033-0000-0000-0000-000000000003"), "MEDIUM", "#f59e0b", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Action required within 24 hours", null, true, false, true, "Medium", 3, 3, new Guid("00000010-0000-0000-0000-000000000003"), null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("00000033-0000-0000-0000-000000000004"), "LOW", "#22c55e", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Informational -- log and monitor", null, true, false, true, "Low", 4, 4, new Guid("00000010-0000-0000-0000-000000000003"), null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) }
                });

            migrationBuilder.InsertData(
                table: "AlertStatuses",
                columns: new[] { "Id", "Code", "CreatedAt", "DeletedAt", "Description", "IndustryTemplateId", "IsActive", "IsDeleted", "IsSystem", "IsTerminal", "Name", "SortOrder", "TemplateId", "TenantId", "UpdatedAt" },
                values: new object[,]
                {
                    { new Guid("00000034-0000-0000-0000-000000000001"), "OPEN", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Alert triggered, no action taken yet", null, true, false, true, false, "Open", 1, new Guid("00000010-0000-0000-0000-000000000003"), null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("00000034-0000-0000-0000-000000000002"), "ACKNOWLEDGED", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Alert seen and noted by operator", null, true, false, true, false, "Acknowledged", 2, new Guid("00000010-0000-0000-0000-000000000003"), null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("00000034-0000-0000-0000-000000000003"), "ASSIGNED", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Alert assigned to an operator for action", null, true, false, true, false, "Assigned", 3, new Guid("00000010-0000-0000-0000-000000000003"), null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("00000034-0000-0000-0000-000000000004"), "RESOLVED", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Issue fixed, alert resolved", null, true, false, true, true, "Resolved", 4, new Guid("00000010-0000-0000-0000-000000000003"), null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("00000034-0000-0000-0000-000000000005"), "CLOSED", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Alert closed after review", null, true, false, true, true, "Closed", 5, new Guid("00000010-0000-0000-0000-000000000003"), null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) }
                });

            migrationBuilder.InsertData(
                table: "DeviceStatuses",
                columns: new[] { "Id", "Code", "Color", "CreatedAt", "DeletedAt", "Description", "IndustryTemplateId", "IsActive", "IsDeleted", "IsSystem", "Name", "SortOrder", "TemplateId", "TenantId", "UpdatedAt" },
                values: new object[,]
                {
                    { new Guid("00000032-0000-0000-0000-000000000001"), "ONLINE", "#22c55e", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Device is operational and sending telemetry", null, true, false, true, "Online", 1, new Guid("00000010-0000-0000-0000-000000000003"), null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("00000032-0000-0000-0000-000000000002"), "OFFLINE", "#ef4444", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Device is not reachable", null, true, false, true, "Offline", 2, new Guid("00000010-0000-0000-0000-000000000003"), null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("00000032-0000-0000-0000-000000000003"), "MAINTENANCE", "#f59e0b", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Device is under scheduled maintenance", null, true, false, true, "Maintenance", 3, new Guid("00000010-0000-0000-0000-000000000003"), null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("00000032-0000-0000-0000-000000000004"), "DECOMMISSIONED", "#6b7280", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Device has been permanently retired", null, true, false, true, "Decommissioned", 4, new Guid("00000010-0000-0000-0000-000000000003"), null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) }
                });

            migrationBuilder.InsertData(
                table: "MetricTypes",
                columns: new[] { "Id", "Code", "CreatedAt", "DefaultUnit", "DeletedAt", "Description", "IndustryTemplateId", "IsActive", "IsDeleted", "IsSystem", "Name", "SortOrder", "TemplateId", "TenantId", "UpdatedAt" },
                values: new object[,]
                {
                    { new Guid("00000035-0000-0000-0000-000000000001"), "TEMPERATURE", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "C", null, "Thermal measurement", null, true, false, true, "Temperature", 1, new Guid("00000010-0000-0000-0000-000000000003"), null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("00000035-0000-0000-0000-000000000002"), "PRESSURE", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "bar", null, "Fluid pressure measurement", null, true, false, true, "Pressure", 2, new Guid("00000010-0000-0000-0000-000000000003"), null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("00000035-0000-0000-0000-000000000003"), "VIBRATION", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "mm/s", null, "Mechanical vibration measurement", null, true, false, true, "Vibration", 3, new Guid("00000010-0000-0000-0000-000000000003"), null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("00000035-0000-0000-0000-000000000004"), "FLOW_RATE", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "L/min", null, "Fluid flow rate measurement", null, true, false, true, "Flow Rate", 4, new Guid("00000010-0000-0000-0000-000000000003"), null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("00000035-0000-0000-0000-000000000005"), "POWER", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "kW", null, "Electrical power consumption", null, true, false, true, "Power Consumption", 5, new Guid("00000010-0000-0000-0000-000000000003"), null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("00000035-0000-0000-0000-000000000006"), "SPEED", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "RPM", null, "Rotational speed measurement", null, true, false, true, "Speed", 6, new Guid("00000010-0000-0000-0000-000000000003"), null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "AlertSeverities",
                keyColumn: "Id",
                keyValue: new Guid("00000033-0000-0000-0000-000000000001"));

            migrationBuilder.DeleteData(
                table: "AlertSeverities",
                keyColumn: "Id",
                keyValue: new Guid("00000033-0000-0000-0000-000000000002"));

            migrationBuilder.DeleteData(
                table: "AlertSeverities",
                keyColumn: "Id",
                keyValue: new Guid("00000033-0000-0000-0000-000000000003"));

            migrationBuilder.DeleteData(
                table: "AlertSeverities",
                keyColumn: "Id",
                keyValue: new Guid("00000033-0000-0000-0000-000000000004"));

            migrationBuilder.DeleteData(
                table: "AlertStatuses",
                keyColumn: "Id",
                keyValue: new Guid("00000034-0000-0000-0000-000000000001"));

            migrationBuilder.DeleteData(
                table: "AlertStatuses",
                keyColumn: "Id",
                keyValue: new Guid("00000034-0000-0000-0000-000000000002"));

            migrationBuilder.DeleteData(
                table: "AlertStatuses",
                keyColumn: "Id",
                keyValue: new Guid("00000034-0000-0000-0000-000000000003"));

            migrationBuilder.DeleteData(
                table: "AlertStatuses",
                keyColumn: "Id",
                keyValue: new Guid("00000034-0000-0000-0000-000000000004"));

            migrationBuilder.DeleteData(
                table: "AlertStatuses",
                keyColumn: "Id",
                keyValue: new Guid("00000034-0000-0000-0000-000000000005"));

            migrationBuilder.DeleteData(
                table: "DeviceStatuses",
                keyColumn: "Id",
                keyValue: new Guid("00000032-0000-0000-0000-000000000001"));

            migrationBuilder.DeleteData(
                table: "DeviceStatuses",
                keyColumn: "Id",
                keyValue: new Guid("00000032-0000-0000-0000-000000000002"));

            migrationBuilder.DeleteData(
                table: "DeviceStatuses",
                keyColumn: "Id",
                keyValue: new Guid("00000032-0000-0000-0000-000000000003"));

            migrationBuilder.DeleteData(
                table: "DeviceStatuses",
                keyColumn: "Id",
                keyValue: new Guid("00000032-0000-0000-0000-000000000004"));

            migrationBuilder.DeleteData(
                table: "MetricTypes",
                keyColumn: "Id",
                keyValue: new Guid("00000035-0000-0000-0000-000000000001"));

            migrationBuilder.DeleteData(
                table: "MetricTypes",
                keyColumn: "Id",
                keyValue: new Guid("00000035-0000-0000-0000-000000000002"));

            migrationBuilder.DeleteData(
                table: "MetricTypes",
                keyColumn: "Id",
                keyValue: new Guid("00000035-0000-0000-0000-000000000003"));

            migrationBuilder.DeleteData(
                table: "MetricTypes",
                keyColumn: "Id",
                keyValue: new Guid("00000035-0000-0000-0000-000000000004"));

            migrationBuilder.DeleteData(
                table: "MetricTypes",
                keyColumn: "Id",
                keyValue: new Guid("00000035-0000-0000-0000-000000000005"));

            migrationBuilder.DeleteData(
                table: "MetricTypes",
                keyColumn: "Id",
                keyValue: new Guid("00000035-0000-0000-0000-000000000006"));

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "MetricTypes",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "MetricTypes",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(300)",
                oldMaxLength: 300,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "DefaultUnit",
                table: "MetricTypes",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20);

            migrationBuilder.AlterColumn<string>(
                name: "Code",
                table: "MetricTypes",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "DeviceStatuses",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "DeviceStatuses",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(300)",
                oldMaxLength: 300,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Color",
                table: "DeviceStatuses",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Code",
                table: "DeviceStatuses",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "AlertStatuses",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "AlertStatuses",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(300)",
                oldMaxLength: 300,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Code",
                table: "AlertStatuses",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "AlertSeverities",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "AlertSeverities",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(300)",
                oldMaxLength: 300,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Color",
                table: "AlertSeverities",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Code",
                table: "AlertSeverities",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);
        }
    }
}
