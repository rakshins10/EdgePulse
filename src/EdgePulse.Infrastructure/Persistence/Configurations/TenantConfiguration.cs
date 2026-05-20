using EdgePulse.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EdgePulse.Infrastructure.Persistence.Configurations;

public class TenantConfiguration : IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> builder)
    {
        builder.ToTable("Tenants");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.Slug)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.ContactEmail)
            .IsRequired()
            .HasMaxLength(300);

        builder.Property(x => x.Status)
            .IsRequired()
            .HasMaxLength(20);

        builder.HasIndex(x => x.Slug).IsUnique();

        // Dev tenant -- matches placeholder CurrentUserService TenantId
        // Removed when Keycloak JWT is implemented
        builder.HasData(new
        {
            Id = Guid.Parse("00000099-0000-0000-0000-000000000001"),
            Name = "EdgePulse Dev Tenant",
            Slug = "edgepulse-dev",
            ContactEmail = "dev@edgepulse.com",
            Status = "Active",
            CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            UpdatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            IsDeleted = false,
            DeletedAt = (DateTime?)null
        });
    }
}
