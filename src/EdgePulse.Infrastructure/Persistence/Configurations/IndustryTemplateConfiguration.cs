using EdgePulse.Domain.Constants;
using EdgePulse.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EdgePulse.Infrastructure.Persistence.Configurations;

public class IndustryTemplateConfiguration
    : IEntityTypeConfiguration<IndustryTemplate>
{
    public void Configure(EntityTypeBuilder<IndustryTemplate> builder)
    {
        builder.ToTable("IndustryTemplates");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.Description)
            .HasMaxLength(500);

        builder.HasIndex(x => x.Name).IsUnique();

        // Seed industry templates
        builder.HasData(
            new
            {
                Id = IndustryTemplateIds.PulpAndPaper,
                Name = "Pulp & Paper",
                Description = "Template for pulp and paper manufacturing facilities",
                IsDefault = false,
                CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                IsDeleted = false,
                DeletedAt = (DateTime?)null
            },
            new
            {
                Id = IndustryTemplateIds.Manufacturing,
                Name = "Manufacturing",
                Description = "Template for general manufacturing facilities",
                IsDefault = false,
                CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                IsDeleted = false,
                DeletedAt = (DateTime?)null
            },
            new
            {
                Id = IndustryTemplateIds.Generic,
                Name = "Generic",
                Description = "Generic template suitable for any industry",
                IsDefault = true,
                CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                IsDeleted = false,
                DeletedAt = (DateTime?)null
            }
        );
    }
}
