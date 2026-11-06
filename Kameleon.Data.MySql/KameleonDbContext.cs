namespace Kameleon.Data.MySql;

/// <summary>
/// Represents the kameleon database context.
/// </summary>
public sealed class KameleonDbContext(DbContextOptions<KameleonDbContext> options) : KameleonDbContextBase(options);
