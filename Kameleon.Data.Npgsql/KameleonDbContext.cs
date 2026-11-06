namespace Kameleon.Data.Npgsql;

/// <summary>
/// Represents the kameleon database context.
/// </summary>
public sealed class KameleonDbContext(DbContextOptions<KameleonDbContext> options) : KameleonDbContextBase(options);
