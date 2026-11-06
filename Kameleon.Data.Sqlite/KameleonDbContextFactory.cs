using Microsoft.Data.Sqlite;

namespace Kameleon.Data.Sqlite;

/// <summary>
/// Represents the kameleon database context factory.
/// </summary>
public sealed class KameleonDbContextFactory(string connectionString) : IKameleonDbContextFactory
{
    private DbContextOptions<KameleonDbContext> Options => field ??= BuildOptions(connectionString);

    /// <summary>
    /// Gets the provider name.
    /// </summary>
    public string ProviderName => Constants.DatabaseProviders.Sqlite;

    /// <summary>
    /// Gets the context.
    /// </summary>
    /// <returns>The context.</returns>
    public DbContext GetContext() => new KameleonDbContext(Options);

    /// <summary>
    /// Gets the context.
    /// </summary>
    /// <param name="connectionString">The connection string.</param>
    /// <returns>The context.</returns>
    public DbContext GetContext(string connectionString) => new KameleonDbContext(BuildOptions(connectionString));

    internal static DbContextOptions<KameleonDbContext> BuildOptions(string connectionString)
    {
        var optionsBuilder = new DbContextOptionsBuilder<KameleonDbContext>();

        optionsBuilder.UseSqlite(Prepare(connectionString), sqlite => sqlite
            .MigrationsAssembly(typeof(KameleonDbContext).Assembly.GetName().Name)
            .MigrationsHistoryTable(Constants.MigrationsHistoryTable));

        return optionsBuilder.Options;
    }

    /// <summary>Creates the database's folder, since SQLite creates a missing file but not a missing directory.</summary>
    private static string Prepare(string connectionString)
    {
        var builder = new SqliteConnectionStringBuilder(connectionString);

        if (builder.DataSource is { Length: > 0 } dataSource
            && !dataSource.Equals(":memory:", StringComparison.OrdinalIgnoreCase)
            && builder.Mode != SqliteOpenMode.Memory
            && Path.GetDirectoryName(Path.GetFullPath(dataSource)) is { Length: > 0 } directory)
        {
            Directory.CreateDirectory(directory);
        }

        return builder.ToString();
    }
}
