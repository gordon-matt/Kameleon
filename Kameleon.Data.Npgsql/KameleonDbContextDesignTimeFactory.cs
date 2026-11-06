using Microsoft.EntityFrameworkCore.Design;

namespace Kameleon.Data.Npgsql;

/// <summary>Used by <c>dotnet ef</c>, so migrations can be generated without a host application.</summary>
public sealed class KameleonDbContextDesignTimeFactory : IDesignTimeDbContextFactory<KameleonDbContext>
{
    /// <summary>
    /// Creates the database context.
    /// </summary>
    /// <param name="args">The args.</param>
    /// <returns>The database context.</returns>
    public KameleonDbContext CreateDbContext(string[] args) =>
        new(KameleonDbContextFactory.BuildOptions(
            "Host=localhost;Database=kameleon_design;Username=postgres;Password=postgres"));
}
