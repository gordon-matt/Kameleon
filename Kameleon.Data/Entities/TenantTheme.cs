namespace Kameleon.Data.Entities;

/// <summary>
/// One theme as seen by one tenant. Two people write to it: an application administrator grants the theme
/// (<see cref="IsGranted"/>), and the tenant's administrator enables and configures it. A tenant with no row
/// for a theme uses the application defaults.
/// </summary>
public class TenantTheme : BaseEntity<int>
{
    /// <summary>
    /// Gets or sets the tenant identifier.
    /// </summary>
    public string TenantId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the theme identifier.
    /// </summary>
    public string ThemeId { get; set; } = string.Empty;

    /// <summary>Only read when the theme's availability is <see cref="ThemeAvailability.SelectedTenants"/>.</summary>
    public bool IsGranted { get; set; }

    /// <summary>Whether the tenant offers this theme. Users can only pick enabled themes.</summary>
    public bool IsEnabled { get; set; }

    /// <summary>The tenant's setting values as a JSON object of strings. Validated again whenever it is read.</summary>
    public string? Configuration { get; set; }

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
/// Represents the tenant theme map.
/// </summary>
public sealed class TenantThemeMap : IEntityTypeConfiguration<TenantTheme>
{
    /// <summary>
    /// Configures the value.
    /// </summary>
    /// <param name="builder">The builder.</param>
    public void Configure(EntityTypeBuilder<TenantTheme> builder)
    {
        builder.ToTable(Constants.Tables.TenantThemes);
        builder.HasKey(m => m.Id);

        builder.Property(m => m.TenantId).IsRequired().HasMaxLength(Constants.MaxLengths.TenantId).IsUnicode(false);
        builder.Property(m => m.ThemeId).IsRequired().HasMaxLength(Constants.MaxLengths.ThemeId).IsUnicode(false);
        builder.Property(m => m.Configuration).IsUnicode(true);
        builder.Property(m => m.UpdatedAtUtc).HasConversion(UtcDateTimeConverter.Instance);
        builder.Property(m => m.UpdatedBy).HasMaxLength(Constants.MaxLengths.UserId).IsUnicode(true);

        builder.HasIndex(m => new { m.TenantId, m.ThemeId }).IsUnique();
        builder.HasIndex(m => m.ThemeId);
    }
}
