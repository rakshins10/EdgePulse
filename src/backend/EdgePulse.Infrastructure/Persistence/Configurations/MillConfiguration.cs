using EdgePulse.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EdgePulse.Infrastructure.Persistence.Configurations;

public class MillConfiguration : IEntityTypeConfiguration<Mill>
{
    public void Configure(EntityTypeBuilder<Mill> builder)
    {
        builder.ToTable("Mills");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.Code)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(x => x.Location)
            .IsRequired()
            .HasMaxLength(300);

        builder.Property(x => x.Timezone)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.DeploymentMode)
            .HasConversion<string>();

        builder.HasIndex(x => new { x.TenantId, x.Code })
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");

        builder.HasOne(x => x.Tenant)
            .WithMany(x => x.Mills)
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
