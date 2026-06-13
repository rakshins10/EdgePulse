using EdgePulse.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EdgePulse.Infrastructure.Persistence.Configurations;

public class UiStringTranslationConfiguration : IEntityTypeConfiguration<UiStringTranslation>
{
    public void Configure(EntityTypeBuilder<UiStringTranslation> builder)
    {
        builder.ToTable("UiStringTranslations");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.LocaleCode).IsRequired().HasMaxLength(10);
        builder.Property(x => x.Key).IsRequired().HasMaxLength(200);
        builder.Property(x => x.Value).IsRequired().HasMaxLength(2000);

        // Primary lookup path: all overrides for a locale (+ tenant)
        builder.HasIndex(x => new { x.LocaleCode, x.TenantId })
            .HasDatabaseName("IX_UiStringTranslations_Locale_Tenant");

        // Uniqueness: one value per (key, locale, tenant) excluding soft-deleted
        builder.HasIndex(x => new { x.Key, x.LocaleCode, x.TenantId })
            .IsUnique()
            .HasFilter("[IsDeleted] = 0")
            .HasDatabaseName("IX_UiStringTranslations_Key_Locale_Tenant");
    }
}
