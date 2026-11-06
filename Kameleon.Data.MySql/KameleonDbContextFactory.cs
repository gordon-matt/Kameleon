namespace Kameleon.Data.MySql;

/// <summary>
/// Represents the kameleon database context factory.
/// </summary>
public sealed class KameleonDbContextFactory(string connectionString) : IKameleonDbContextFactory
{
    private DbContextOptions<KameleonDbContext> Options => field ??= BuildOptions(connectionString);

    /// <summary>
    /// Gets the provider name.
    /// </summary>
    public string ProviderName => Constants.DatabaseProviders.MySql;

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

        optionsBuilder.UseMySQL(connectionString, mySql => mySql
            .MigrationsAssembly(typeof(KameleonDbContext).Assembly.GetName().Name)
            .MigrationsHistoryTable(Constants.MigrationsHistoryTable));

        return optionsBuilder.Options;
    }
}
