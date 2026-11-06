namespace Kameleon.Data;

/// <summary>
/// Provider-agnostic context. Each <c>Kameleon.Data.{Provider}</c> project subclasses this and owns
/// its own migrations, because EF bakes provider-specific column types into them.
/// </summary>
public abstract class KameleonDbContextBase(DbContextOptions options) : DbContext(options)
{
    /// <summary>
    /// Gets the catalog entries.
    /// </summary>
    public DbSet<ThemeCatalogEntry> CatalogEntries => Set<ThemeCatalogEntry>();

    /// <summary>
    /// Gets the tenant settings.
    /// </summary>
    public DbSet<TenantThemeSettings> TenantSettings => Set<TenantThemeSettings>();

    /// <summary>
    /// Gets the tenant themes.
    /// </summary>
    public DbSet<TenantTheme> TenantThemes => Set<TenantTheme>();

    /// <summary>
    /// Gets the user preferences.
    /// </summary>
    public DbSet<UserThemePreference> UserPreferences => Set<UserThemePreference>();

    /// <inheritdoc/>
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.ApplyConfigurationsFromAssembly(typeof(KameleonDbContextBase).Assembly);
    }
}
