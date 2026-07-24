using EdgePulse.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EdgePulse.Infrastructure.Persistence.Configurations;

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("Notifications");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Type)
            .IsRequired()
            .HasMaxLength(30);

        builder.Property(x => x.SeverityCode)
            .HasMaxLength(20);

        builder.Property(x => x.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.Message)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(x => x.LinkEntityType)
            .HasMaxLength(50);

        // The notification bell always queries "this tenant, unread first,
        // newest first" — one covering index serves it.
        builder.HasIndex(x => new { x.TenantId, x.IsRead, x.CreatedAt })
            .HasDatabaseName("IX_Notifications_Tenant_IsRead_CreatedAt");
    }
}
