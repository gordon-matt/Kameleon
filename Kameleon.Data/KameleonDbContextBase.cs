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

    /// <summary>
    /// Whether the provider can hold the tables in <see cref="Constants.Schema"/>. Providers where a
    /// schema is a database (MySQL) or does not exist (SQLite) return <see langword="false"/>, and
    /// get <see cref="Constants.TablePrefix"/> on every table name instead.
    /// </summary>
    protected virtual bool SupportsSchemas => true;

    /// <inheritdoc/>
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.ApplyConfigurationsFromAssembly(typeof(KameleonDbContextBase).Assembly);

        if (SupportsSchemas)
        {
            builder.HasDefaultSchema(Constants.Schema);
            return;
        }

        builder.Model.SetDefaultSchema(null);

        foreach (var entityType in builder.Model.GetEntityTypes().Where(e => e.BaseType is null))
        {
            if (entityType.GetTableName() is not { } tableName)
            {
                continue;
            }

            entityType.SetSchema(null);
            entityType.SetTableName($"{Constants.TablePrefix}{tableName}");
        }
    }
}
