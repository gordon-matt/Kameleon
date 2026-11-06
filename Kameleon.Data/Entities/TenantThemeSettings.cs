namespace Kameleon.Data.Entities;

/// <summary>One row per tenant. A tenant with no row uses the defaults in the application's options.</summary>
public class TenantThemeSettings : BaseEntity<int>
{
    /// <summary>
    /// Gets or sets the tenant identifier.
    /// </summary>
    public string TenantId { get; set; } = string.Empty;

    /// <summary>
    /// Kept even when the theme is later removed or taken away, so that it comes back if the theme does.
    /// Resolution falls back while it is unavailable; nothing rewrites it during rendering.
    /// </summary>
    public string? DefaultThemeId { get; set; }

    /// <summary>A layout of <see cref="DefaultThemeId"/>. Null means that theme's default layout.</summary>
    public string? DefaultLayoutId { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this instance is allow user override.
    /// </summary>
    public bool AllowUserOverride { get; set; }

    /// <summary>
    /// Gets or sets the updated at utc.
    /// </summary>
    public DateTime UpdatedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the updated by.
    /// </summary>
    public string? UpdatedBy { get; set; }
}

/// <summary>
/// Represents the tenant theme settings map.
/// </summary>
public sealed class TenantThemeSettingsMap : IEntityTypeConfiguration<TenantThemeSettings>
{
    /// <summary>
    /// Configures the value.
    /// </summary>
    /// <param name="builder">The builder.</param>
    public void Configure(EntityTypeBuilder<TenantThemeSettings> builder)
    {
        builder.ToTable(Constants.Tables.TenantSettings);
        builder.HasKey(m => m.Id);

        builder.Property(m => m.TenantId).IsRequired().HasMaxLength(Constants.MaxLengths.TenantId).IsUnicode(false);
        builder.Property(m => m.DefaultThemeId).HasMaxLength(Constants.MaxLengths.ThemeId).IsUnicode(false);
        builder.Property(m => m.DefaultLayoutId).HasMaxLength(Constants.MaxLengths.LayoutId).IsUnicode(false);
        builder.Property(m => m.UpdatedAtUtc).HasConversion(UtcDateTimeConverter.Instance);
        builder.Property(m => m.UpdatedBy).HasMaxLength(Constants.MaxLengths.UserId).IsUnicode(true);

        builder.HasIndex(m => m.TenantId).IsUnique();
    }
}
