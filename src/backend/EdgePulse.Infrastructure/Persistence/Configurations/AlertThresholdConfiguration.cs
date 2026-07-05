using EdgePulse.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EdgePulse.Infrastructure.Persistence.Configurations;

public class AlertThresholdConfiguration
    : IEntityTypeConfiguration<AlertThreshold>
{
    public void Configure(EntityTypeBuilder<AlertThreshold> builder)
    {
        builder.ToTable("AlertThresholds");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.MetricKey)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.SeverityCode)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(x => x.Unit)
            .HasMaxLength(20);

        builder.Property(x => x.Description)
            .HasMaxLength(500);

        // Index for fast threshold lookup in TelemetryProcessor
        builder.HasIndex(x => new { x.DeviceId, x.MetricKey, x.IsActive })
            .HasDatabaseName("IX_AlertThresholds_Device_Metric_Active");

        builder.HasIndex(x => x.TenantId)
            .HasDatabaseName("IX_AlertThresholds_TenantId");

        builder.HasOne(x => x.Device)
            .WithMany()
            .HasForeignKey(x => x.DeviceId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
