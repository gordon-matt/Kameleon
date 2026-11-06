namespace Kameleon.Data.Sql;

/// <summary>
/// Represents the kameleon database context.
/// </summary>
public sealed class KameleonDbContext(DbContextOptions<KameleonDbContext> options) : KameleonDbContextBase(options);
