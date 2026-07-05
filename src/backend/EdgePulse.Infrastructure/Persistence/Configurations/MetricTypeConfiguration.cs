using EdgePulse.Domain.Constants;
using EdgePulse.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EdgePulse.Infrastructure.Persistence.Configurations;

public class MetricTypeConfiguration
    : IEntityTypeConfiguration<MetricType>
{
    private static readonly Guid TemperatureId =
        Guid.Parse("00000035-0000-0000-0000-000000000001");
    private static readonly Guid PressureId =
        Guid.Parse("00000035-0000-0000-0000-000000000002");
    private static readonly Guid VibrationId =
        Guid.Parse("00000035-0000-0000-0000-000000000003");
    private static readonly Guid FlowRateId =
        Guid.Parse("00000035-0000-0000-0000-000000000004");
    private static readonly Guid PowerId =
        Guid.Parse("00000035-0000-0000-0000-000000000005");
    private static readonly Guid SpeedId =
        Guid.Parse("00000035-0000-0000-0000-000000000006");

    public void Configure(EntityTypeBuilder<MetricType> builder)
    {
        builder.ToTable("MetricTypes");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name).IsRequired().HasMaxLength(100);
        builder.Property(x => x.Code).IsRequired().HasMaxLength(50);
        builder.Property(x => x.DefaultUnit).IsRequired().HasMaxLength(20);
        builder.Property(x => x.Description).HasMaxLength(300);

        builder.HasData(
            new
            {
                Id = TemperatureId,
                TemplateId = (Guid?)IndustryTemplateIds.Generic,
                TenantId = (Guid?)null,
                Name = "Temperature", Code = "TEMPERATURE",
                DefaultUnit = "C", Description = "Thermal measurement",
                IsSystem = true, IsActive = true, SortOrder = 1,
                IsDeleted = false, DeletedAt = (DateTime?)null,
                CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new
            {
                Id = PressureId,
                TemplateId = (Guid?)IndustryTemplateIds.Generic,
                TenantId = (Guid?)null,
                Name = "Pressure", Code = "PRESSURE",
                DefaultUnit = "bar", Description = "Fluid pressure measurement",
                IsSystem = true, IsActive = true, SortOrder = 2,
                IsDeleted = false, DeletedAt = (DateTime?)null,
                CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new
            {
                Id = VibrationId,
                TemplateId = (Guid?)IndustryTemplateIds.Generic,
                TenantId = (Guid?)null,
                Name = "Vibration", Code = "VIBRATION",
                DefaultUnit = "mm/s", Description = "Mechanical vibration measurement",
                IsSystem = true, IsActive = true, SortOrder = 3,
                IsDeleted = false, DeletedAt = (DateTime?)null,
                CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new
            {
                Id = FlowRateId,
                TemplateId = (Guid?)IndustryTemplateIds.Generic,
                TenantId = (Guid?)null,
                Name = "Flow Rate", Code = "FLOW_RATE",
                DefaultUnit = "L/min", Description = "Fluid flow rate measurement",
                IsSystem = true, IsActive = true, SortOrder = 4,
                IsDeleted = false, DeletedAt = (DateTime?)null,
                CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new
            {
                Id = PowerId,
                TemplateId = (Guid?)IndustryTemplateIds.Generic,
                TenantId = (Guid?)null,
                Name = "Power Consumption", Code = "POWER",
                DefaultUnit = "kW", Description = "Electrical power consumption",
                IsSystem = true, IsActive = true, SortOrder = 5,
                IsDeleted = false, DeletedAt = (DateTime?)null,
                CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new
            {
                Id = SpeedId,
                TemplateId = (Guid?)IndustryTemplateIds.Generic,
                TenantId = (Guid?)null,
                Name = "Speed", Code = "SPEED",
                DefaultUnit = "RPM", Description = "Rotational speed measurement",
                IsSystem = true, IsActive = true, SortOrder = 6,
                IsDeleted = false, DeletedAt = (DateTime?)null,
                CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            }
        );
    }
}
