using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Kameleon.Data.MySql;

public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>Stores Kameleon's tenant and user theme settings in MySQL.</summary>
        public IServiceCollection AddKameleonMySql(string connectionString)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

            services.RemoveAll<IKameleonDbContextFactory>();
            services.AddSingleton<IKameleonDbContextFactory>(new KameleonDbContextFactory(connectionString));

            return services;
        }
    }
}
