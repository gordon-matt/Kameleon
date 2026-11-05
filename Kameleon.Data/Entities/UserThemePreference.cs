namespace Kameleon.Data.Entities;

/// <summary>
/// A user's chosen theme in one tenant. Per tenant because each tenant offers different themes, so a person
/// who belongs to two sites can choose differently in each. No row means the user follows the tenant default.
/// </summary>
public class UserThemePreference : BaseEntity<int>
{
    public string TenantId { get; set; } = string.Empty;

    public string UserId { get; set; } = string.Empty;

    public string ThemeId { get; set; } = string.Empty;

    public DateTime UpdatedAtUtc { get; set; }
}

public sealed class UserThemePreferenceMap : IEntityTypeConfiguration<UserThemePreference>
{
    public void Configure(EntityTypeBuilder<UserThemePreference> builder)
    {
        builder.ToTable(Constants.Tables.UserPreferences);
        builder.HasKey(m => m.Id);

        builder.Property(m => m.TenantId).IsRequired().HasMaxLength(Constants.MaxLengths.TenantId).IsUnicode(false);
        builder.Property(m => m.UserId).IsRequired().HasMaxLength(Constants.MaxLengths.UserId).IsUnicode(true);
        builder.Property(m => m.ThemeId).IsRequired().HasMaxLength(Constants.MaxLengths.ThemeId).IsUnicode(false);
        builder.Property(m => m.UpdatedAtUtc).HasConversion(UtcDateTimeConverter.Instance);

        builder.HasIndex(m => new { m.TenantId, m.UserId }).IsUnique();
    }
}
