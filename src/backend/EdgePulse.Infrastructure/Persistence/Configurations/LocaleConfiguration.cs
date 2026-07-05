using EdgePulse.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EdgePulse.Infrastructure.Persistence.Configurations;

public class LocaleConfiguration : IEntityTypeConfiguration<Locale>
{
    // Fixed IDs + static timestamp so HasData is deterministic across migrations.
    private static readonly DateTime SeedTimestamp =
        new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    public void Configure(EntityTypeBuilder<Locale> builder)
    {
        builder.ToTable("Locales");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Code).IsRequired().HasMaxLength(10);
        builder.Property(x => x.DisplayName).IsRequired().HasMaxLength(100);
        builder.Property(x => x.NativeName).IsRequired().HasMaxLength(100);
        builder.Property(x => x.Flag).HasMaxLength(20);

        // One row per language code (excluding soft-deleted)
        builder.HasIndex(x => x.Code)
            .IsUnique()
            .HasFilter("[IsDeleted] = 0")
            .HasDatabaseName("IX_Locales_Code");

        // Seed the three launch locales so fresh databases have them.
        builder.HasData(
            SeedRow("a0000001-0000-0000-0000-000000000001", "en", "English", "English", "🇬🇧", true, 10),
            SeedRow("a0000001-0000-0000-0000-000000000002", "fi", "Finnish", "Suomi",   "🇫🇮", false, 20),
            SeedRow("a0000001-0000-0000-0000-000000000003", "sv", "Swedish", "Svenska", "🇸🇪", false, 30));
    }

    private static object SeedRow(
        string id, string code, string displayName, string nativeName,
        string flag, bool isDefault, int sortOrder) => new
    {
        Id = Guid.Parse(id),
        Code = code,
        DisplayName = displayName,
        NativeName = nativeName,
        Flag = flag,
        IsEnabled = true,
        IsDefault = isDefault,
        SortOrder = sortOrder,
        CreatedAt = SeedTimestamp,
        UpdatedAt = SeedTimestamp,
        IsDeleted = false,
    };
}
