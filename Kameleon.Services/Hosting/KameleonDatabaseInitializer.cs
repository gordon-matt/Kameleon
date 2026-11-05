using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;

namespace Kameleon.Hosting;

/// <summary>Applies Kameleon's migrations at startup when a database provider is registered.</summary>
internal sealed class KameleonDatabaseInitializer(
    ILogger<KameleonDatabaseInitializer> logger,
    IServiceProvider services,
    IOptions<KameleonOptions> options) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!options.Value.AutoMigrate || services.GetService<IKameleonDbContextFactory>() is not { } factory)
        {
            return;
        }

        await using var context = factory.GetContext();
        await context.Database.MigrateAsync(cancellationToken);
        logger.LogInformation("Kameleon database is up to date ({Provider})", factory.ProviderName);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
