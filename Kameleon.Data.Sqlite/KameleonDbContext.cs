namespace Kameleon.Data.Sqlite;

/// <summary>
/// Represents the kameleon database context.
/// </summary>
public sealed class KameleonDbContext(DbContextOptions<KameleonDbContext> options) : KameleonDbContextBase(options)
{
    /// <inheritdoc/>
    protected override bool SupportsSchemas => false;
}
