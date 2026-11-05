using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Kameleon.Data.Sql;

public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>Stores Kameleon's tenant and user theme settings in SQL Server.</summary>
        public IServiceCollection AddKameleonSqlServer(string connectionString)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

            services.RemoveAll<IKameleonDbContextFactory>();
            services.AddSingleton<IKameleonDbContextFactory>(new KameleonDbContextFactory(connectionString));

            return services;
        }
    }
}
