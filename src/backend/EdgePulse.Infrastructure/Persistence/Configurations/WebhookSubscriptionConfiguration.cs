using EdgePulse.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EdgePulse.Infrastructure.Persistence.Configurations;

public class WebhookSubscriptionConfiguration : IEntityTypeConfiguration<WebhookSubscription>
{
    public void Configure(EntityTypeBuilder<WebhookSubscription> builder)
    {
        builder.ToTable("WebhookSubscriptions");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name).IsRequired().HasMaxLength(100);
        builder.Property(x => x.Url).IsRequired().HasMaxLength(500);
        builder.Property(x => x.Secret).IsRequired().HasMaxLength(200);
        builder.Property(x => x.Events).IsRequired().HasMaxLength(500);
        builder.Property(x => x.Format).IsRequired().HasMaxLength(10);
        builder.Property(x => x.LastStatus).HasMaxLength(50);

        builder.HasIndex(x => new { x.TenantId, x.IsActive })
            .HasDatabaseName("IX_WebhookSubscriptions_Tenant_Active");
    }
}
