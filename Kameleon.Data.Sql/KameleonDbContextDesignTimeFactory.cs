using Microsoft.EntityFrameworkCore.Design;

namespace Kameleon.Data.Sql;

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
            "Server=(localdb)\\MSSQLLocalDB;Database=Kameleon_Design;Trusted_Connection=True;TrustServerCertificate=True"));
}
