using EdgePulse.Domain.Constants;
using EdgePulse.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EdgePulse.Infrastructure.Persistence.Configurations;

public class AlertSeverityConfiguration
    : IEntityTypeConfiguration<AlertSeverity>
{
    public void Configure(EntityTypeBuilder<AlertSeverity> builder)
    {
        builder.ToTable("AlertSeverities");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name).IsRequired().HasMaxLength(100);
        builder.Property(x => x.Code).IsRequired().HasMaxLength(50);
        builder.Property(x => x.Description).HasMaxLength(300);
        builder.Property(x => x.Color).HasMaxLength(20);

        builder.HasData(
            new
            {
                Id = GenericAlertSeverityIds.Critical,
                TemplateId = (Guid?)IndustryTemplateIds.Generic,
                TenantId = (Guid?)null,
                Name = "Critical", Code = "CRITICAL",
                Description = "Immediate action required -- machine must stop",
                Color = "#ef4444", Priority = 1,
                IsSystem = true, IsActive = true, SortOrder = 1,
                IsDeleted = false, DeletedAt = (DateTime?)null,
                CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new
            {
                Id = GenericAlertSeverityIds.High,
                TemplateId = (Guid?)IndustryTemplateIds.Generic,
                TenantId = (Guid?)null,
                Name = "High", Code = "HIGH",
                Description = "Action required within 1 hour",
                Color = "#f97316", Priority = 2,
                IsSystem = true, IsActive = true, SortOrder = 2,
                IsDeleted = false, DeletedAt = (DateTime?)null,
                CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new
            {
                Id = GenericAlertSeverityIds.Medium,
                TemplateId = (Guid?)IndustryTemplateIds.Generic,
                TenantId = (Guid?)null,
                Name = "Medium", Code = "MEDIUM",
                Description = "Action required within 24 hours",
                Color = "#f59e0b", Priority = 3,
                IsSystem = true, IsActive = true, SortOrder = 3,
                IsDeleted = false, DeletedAt = (DateTime?)null,
                CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new
            {
                Id = GenericAlertSeverityIds.Low,
                TemplateId = (Guid?)IndustryTemplateIds.Generic,
                TenantId = (Guid?)null,
                Name = "Low", Code = "LOW",
                Description = "Informational -- log and monitor",
                Color = "#22c55e", Priority = 4,
                IsSystem = true, IsActive = true, SortOrder = 4,
                IsDeleted = false, DeletedAt = (DateTime?)null,
                CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            }
        );
    }
}
