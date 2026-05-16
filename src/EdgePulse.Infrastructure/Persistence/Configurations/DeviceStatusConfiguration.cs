using EdgePulse.Domain.Constants;
using EdgePulse.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EdgePulse.Infrastructure.Persistence.Configurations;

public class DeviceStatusConfiguration
    : IEntityTypeConfiguration<DeviceStatus>
{
    public void Configure(EntityTypeBuilder<DeviceStatus> builder)
    {
        builder.ToTable("DeviceStatuses");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name).IsRequired().HasMaxLength(100);
        builder.Property(x => x.Code).IsRequired().HasMaxLength(50);
        builder.Property(x => x.Description).HasMaxLength(300);
        builder.Property(x => x.Color).HasMaxLength(20);

        builder.HasData(
            new
            {
                Id = GenericDeviceStatusIds.Online,
                TemplateId = (Guid?)IndustryTemplateIds.Generic,
                TenantId = (Guid?)null,
                Name = "Online", Code = "ONLINE",
                Description = "Device is operational and sending telemetry",
                Color = "#22c55e", IsSystem = true, IsActive = true,
                SortOrder = 1, IsDeleted = false, DeletedAt = (DateTime?)null,
                CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new
            {
                Id = GenericDeviceStatusIds.Offline,
                TemplateId = (Guid?)IndustryTemplateIds.Generic,
                TenantId = (Guid?)null,
                Name = "Offline", Code = "OFFLINE",
                Description = "Device is not reachable",
                Color = "#ef4444", IsSystem = true, IsActive = true,
                SortOrder = 2, IsDeleted = false, DeletedAt = (DateTime?)null,
                CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new
            {
                Id = GenericDeviceStatusIds.Maintenance,
                TemplateId = (Guid?)IndustryTemplateIds.Generic,
                TenantId = (Guid?)null,
                Name = "Maintenance", Code = "MAINTENANCE",
                Description = "Device is under scheduled maintenance",
                Color = "#f59e0b", IsSystem = true, IsActive = true,
                SortOrder = 3, IsDeleted = false, DeletedAt = (DateTime?)null,
                CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new
            {
                Id = GenericDeviceStatusIds.Decommissioned,
                TemplateId = (Guid?)IndustryTemplateIds.Generic,
                TenantId = (Guid?)null,
                Name = "Decommissioned", Code = "DECOMMISSIONED",
                Description = "Device has been permanently retired",
                Color = "#6b7280", IsSystem = true, IsActive = true,
                SortOrder = 4, IsDeleted = false, DeletedAt = (DateTime?)null,
                CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            }
        );
    }
}
