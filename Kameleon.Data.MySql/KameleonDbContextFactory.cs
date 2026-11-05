namespace Kameleon.Data.MySql;

public sealed class KameleonDbContextFactory(string connectionString) : IKameleonDbContextFactory
{
    private DbContextOptions<KameleonDbContext> Options => field ??= BuildOptions(connectionString);

    public string ProviderName => Constants.DatabaseProviders.MySql;

    public DbContext GetContext() => new KameleonDbContext(Options);

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
