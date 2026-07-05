using EdgePulse.Domain.Constants;
using EdgePulse.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EdgePulse.Infrastructure.Persistence.Configurations;

public class DeviceTypeConfiguration
    : IEntityTypeConfiguration<DeviceType>
{
    public void Configure(EntityTypeBuilder<DeviceType> builder)
    {
        builder.ToTable("DeviceTypes");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.Code)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.Description)
            .HasMaxLength(300);

        builder.Property(x => x.Icon)
            .HasMaxLength(50);

        builder.HasIndex(x => new { x.TemplateId, x.Code })
            .IsUnique()
            .HasFilter("[TemplateId] IS NOT NULL");

        builder.HasIndex(x => new { x.TenantId, x.Code })
            .IsUnique()
            .HasFilter("[TenantId] IS NOT NULL");

        // Seed Pulp & Paper template device types
        builder.HasData(
            new
            {
                Id = PulpAndPaperDeviceTypeIds.Pump,
                TemplateId = (Guid?)IndustryTemplateIds.PulpAndPaper,
                TenantId = (Guid?)null,
                Name = "Pump",
                Code = "PUMP",
                Icon = "pump",
                Description = "Fluid pumping equipment",
                IsSystem = true,
                IsActive = true,
                SortOrder = 1,
                CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                IsDeleted = false,
                DeletedAt = (DateTime?)null
            },
            new
            {
                Id = PulpAndPaperDeviceTypeIds.Motor,
                TemplateId = (Guid?)IndustryTemplateIds.PulpAndPaper,
                TenantId = (Guid?)null,
                Name = "Motor",
                Code = "MOTOR",
                Icon = "motor",
                Description = "Electric drive motor",
                IsSystem = true,
                IsActive = true,
                SortOrder = 2,
                CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                IsDeleted = false,
                DeletedAt = (DateTime?)null
            },
            new
            {
                Id = PulpAndPaperDeviceTypeIds.Valve,
                TemplateId = (Guid?)IndustryTemplateIds.PulpAndPaper,
                TenantId = (Guid?)null,
                Name = "Valve",
                Code = "VALVE",
                Icon = "valve",
                Description = "Flow control valve",
                IsSystem = true,
                IsActive = true,
                SortOrder = 3,
                CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                IsDeleted = false,
                DeletedAt = (DateTime?)null
            },
            new
            {
                Id = PulpAndPaperDeviceTypeIds.Digester,
                TemplateId = (Guid?)IndustryTemplateIds.PulpAndPaper,
                TenantId = (Guid?)null,
                Name = "Digester",
                Code = "DIGESTER",
                Icon = "digester",
                Description = "Chemical pulp digestion vessel",
                IsSystem = true,
                IsActive = true,
                SortOrder = 4,
                CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                IsDeleted = false,
                DeletedAt = (DateTime?)null
            },
            new
            {
                Id = PulpAndPaperDeviceTypeIds.Refiner,
                TemplateId = (Guid?)IndustryTemplateIds.PulpAndPaper,
                TenantId = (Guid?)null,
                Name = "Refiner",
                Code = "REFINER",
                Icon = "refiner",
                Description = "Pulp refining equipment",
                IsSystem = true,
                IsActive = true,
                SortOrder = 5,
                CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                IsDeleted = false,
                DeletedAt = (DateTime?)null
            }
        );
    }
}
