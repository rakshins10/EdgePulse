using EdgePulse.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EdgePulse.Infrastructure.Persistence.Configurations;

public class DeviceApiKeyConfiguration
    : IEntityTypeConfiguration<DeviceApiKey>
{
    public void Configure(EntityTypeBuilder<DeviceApiKey> builder)
    {
        builder.ToTable("DeviceApiKeys");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.KeyHash)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(x => x.KeyPrefix)
            .IsRequired()
            .HasMaxLength(10);

        builder.Property(x => x.RevokedReason)
            .HasMaxLength(200);

        builder.HasIndex(x => x.KeyHash)
            .IsUnique();

        builder.HasOne(x => x.Device)
            .WithMany()
            .HasForeignKey(x => x.DeviceId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
