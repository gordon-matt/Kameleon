namespace Kameleon.Data;

/// <summary>
/// Provider-agnostic context. Each <c>Kameleon.Data.{Provider}</c> project subclasses this and owns
/// its own migrations, because EF bakes provider-specific column types into them.
/// </summary>
public abstract class KameleonDbContextBase(DbContextOptions options) : DbContext(options)
{
    public DbSet<ThemeCatalogEntry> CatalogEntries => Set<ThemeCatalogEntry>();

    public DbSet<TenantThemeSettings> TenantSettings => Set<TenantThemeSettings>();

    public DbSet<TenantTheme> TenantThemes => Set<TenantTheme>();

    public DbSet<UserThemePreference> UserPreferences => Set<UserThemePreference>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.ApplyConfigurationsFromAssembly(typeof(KameleonDbContextBase).Assembly);
    }
}
