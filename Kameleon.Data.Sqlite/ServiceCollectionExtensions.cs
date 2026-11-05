using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Kameleon.Data.Sqlite;

public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>Stores Kameleon's tenant and user theme settings in SQLite.</summary>
        /// <param name="connectionString">
        /// A SQLite connection string. When omitted, the settings go to <c>Kameleon.db</c> under the user's
        /// local application data folder, which survives redeploying the app's own folder.
        /// </param>
        public IServiceCollection AddKameleonSqlite(string? connectionString = null)
        {
            string resolved = string.IsNullOrWhiteSpace(connectionString)
                ? $"Data Source={DefaultDatabasePath()}"
                : connectionString;

            services.RemoveAll<IKameleonDbContextFactory>();
            services.AddSingleton<IKameleonDbContextFactory>(new KameleonDbContextFactory(resolved));

            return services;
        }
    }

    private static string DefaultDatabasePath()
    {
        string appName = Assembly.GetEntryAssembly()?.GetName().Name ?? "Kameleon";

        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            appName,
            "Kameleon.db");
    }
}
