using Microsoft.EntityFrameworkCore.Design;

namespace Kameleon.Data.MySql;

/// <summary>Used by <c>dotnet ef</c>, so migrations can be generated without a host application.</summary>
public sealed class KameleonDbContextDesignTimeFactory : IDesignTimeDbContextFactory<KameleonDbContext>
{
    public KameleonDbContext CreateDbContext(string[] args) =>
        new(KameleonDbContextFactory.BuildOptions(
            "Server=localhost;Database=kameleon_design;User ID=root;Password=root"));
}
