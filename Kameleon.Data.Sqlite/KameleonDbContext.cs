namespace Kameleon.Data.Sqlite;

public sealed class KameleonDbContext(DbContextOptions<KameleonDbContext> options) : KameleonDbContextBase(options);
