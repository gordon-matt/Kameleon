namespace Kameleon.Data.Sql;

public sealed class KameleonDbContext(DbContextOptions<KameleonDbContext> options) : KameleonDbContextBase(options);
