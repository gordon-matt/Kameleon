namespace Kameleon.Data.Entities;

/// <summary>
/// Application-wide decisions about one installed theme. A theme with no row is enabled and has the
/// configured default availability.
/// </summary>
public class ThemeCatalogEntry : BaseEntity<int>
{
    public string ThemeId { get; set; } = string.Empty;

    /// <summary>False takes the theme away from every tenant without uninstalling it.</summary>
    public bool IsEnabled { get; set; } = true;

    public ThemeAvailability Availability { get; set; }

    public DateTime UpdatedAtUtc { get; set; }

    public string? UpdatedBy { get; set; }
}

public sealed class ThemeCatalogEntryMap : IEntityTypeConfiguration<ThemeCatalogEntry>
{
    public void Configure(EntityTypeBuilder<ThemeCatalogEntry> builder)
    {
        builder.ToTable(Constants.Tables.CatalogEntries);
        builder.HasKey(m => m.Id);

        builder.Property(m => m.ThemeId).IsRequired().HasMaxLength(Constants.MaxLengths.ThemeId).IsUnicode(false);
        builder.Property(m => m.UpdatedAtUtc).HasConversion(UtcDateTimeConverter.Instance);
        builder.Property(m => m.UpdatedBy).HasMaxLength(Constants.MaxLengths.UserId).IsUnicode(true);

        builder.HasIndex(m => m.ThemeId).IsUnique();
    }
}
