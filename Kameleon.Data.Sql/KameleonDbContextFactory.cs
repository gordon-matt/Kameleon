namespace Kameleon.Data.Sql;

public sealed class KameleonDbContextFactory(string connectionString) : IKameleonDbContextFactory
{
    private DbContextOptions<KameleonDbContext> Options => field ??= BuildOptions(connectionString);

    public string ProviderName => Constants.DatabaseProviders.SqlServer;

    public DbContext GetContext() => new KameleonDbContext(Options);

    public DbContext GetContext(string connectionString) => new KameleonDbContext(BuildOptions(connectionString));

    internal static DbContextOptions<KameleonDbContext> BuildOptions(string connectionString)
    {
        var optionsBuilder = new DbContextOptionsBuilder<KameleonDbContext>();

        optionsBuilder.UseSqlServer(connectionString, sql => sql
            .MigrationsAssembly(typeof(KameleonDbContext).Assembly.GetName().Name)
            .MigrationsHistoryTable(Constants.MigrationsHistoryTable));

        return optionsBuilder.Options;
    }
}
