using EdgePulse.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EdgePulse.Infrastructure.Persistence.Configurations;

public class LookupTranslationConfiguration : IEntityTypeConfiguration<LookupTranslation>
{
    public void Configure(EntityTypeBuilder<LookupTranslation> builder)
    {
        builder.ToTable("LookupTranslations");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.LookupType).IsRequired().HasMaxLength(50);
        builder.Property(x => x.LocaleCode).IsRequired().HasMaxLength(10);
        builder.Property(x => x.Name).IsRequired().HasMaxLength(200);
        builder.Property(x => x.Description).HasMaxLength(500);

        // Primary lookup path: resolve all translations for a type in a locale
        builder.HasIndex(x => new { x.LookupType, x.LocaleCode, x.TenantId })
            .HasDatabaseName("IX_LookupTranslations_Type_Locale_Tenant");

        // Uniqueness: one translation per (item, locale, tenant) excluding soft-deleted
        builder.HasIndex(x => new { x.ItemId, x.LocaleCode, x.TenantId })
            .IsUnique()
            .HasFilter("[IsDeleted] = 0")
            .HasDatabaseName("IX_LookupTranslations_Item_Locale_Tenant");
    }
}
