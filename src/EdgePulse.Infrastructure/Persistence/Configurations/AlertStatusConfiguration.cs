using EdgePulse.Domain.Constants;
using EdgePulse.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EdgePulse.Infrastructure.Persistence.Configurations;

public class AlertStatusConfiguration
    : IEntityTypeConfiguration<AlertStatus>
{
    public void Configure(EntityTypeBuilder<AlertStatus> builder)
    {
        builder.ToTable("AlertStatuses");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name).IsRequired().HasMaxLength(100);
        builder.Property(x => x.Code).IsRequired().HasMaxLength(50);
        builder.Property(x => x.Description).HasMaxLength(300);

        builder.HasData(
            new
            {
                Id = GenericAlertStatusIds.Open,
                TemplateId = (Guid?)IndustryTemplateIds.Generic,
                TenantId = (Guid?)null,
                Name = "Open", Code = "OPEN",
                Description = "Alert triggered, no action taken yet",
                IsTerminal = false, IsSystem = true, IsActive = true,
                SortOrder = 1, IsDeleted = false, DeletedAt = (DateTime?)null,
                CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new
            {
                Id = GenericAlertStatusIds.Acknowledged,
                TemplateId = (Guid?)IndustryTemplateIds.Generic,
                TenantId = (Guid?)null,
                Name = "Acknowledged", Code = "ACKNOWLEDGED",
                Description = "Alert seen and noted by operator",
                IsTerminal = false, IsSystem = true, IsActive = true,
                SortOrder = 2, IsDeleted = false, DeletedAt = (DateTime?)null,
                CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new
            {
                Id = GenericAlertStatusIds.Assigned,
                TemplateId = (Guid?)IndustryTemplateIds.Generic,
                TenantId = (Guid?)null,
                Name = "Assigned", Code = "ASSIGNED",
                Description = "Alert assigned to an operator for action",
                IsTerminal = false, IsSystem = true, IsActive = true,
                SortOrder = 3, IsDeleted = false, DeletedAt = (DateTime?)null,
                CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new
            {
                Id = GenericAlertStatusIds.Resolved,
                TemplateId = (Guid?)IndustryTemplateIds.Generic,
                TenantId = (Guid?)null,
                Name = "Resolved", Code = "RESOLVED",
                Description = "Issue fixed, alert resolved",
                IsTerminal = true, IsSystem = true, IsActive = true,
                SortOrder = 4, IsDeleted = false, DeletedAt = (DateTime?)null,
                CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new
            {
                Id = GenericAlertStatusIds.Closed,
                TemplateId = (Guid?)IndustryTemplateIds.Generic,
                TenantId = (Guid?)null,
                Name = "Closed", Code = "CLOSED",
                Description = "Alert closed after review",
                IsTerminal = true, IsSystem = true, IsActive = true,
                SortOrder = 5, IsDeleted = false, DeletedAt = (DateTime?)null,
                CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            }
        );
    }
}
