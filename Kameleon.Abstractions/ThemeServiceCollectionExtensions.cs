using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Kameleon;

/// <summary>
/// Provides extension methods for theme service collections.
/// </summary>
public static class ThemeServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Adds a compiled theme to the catalog. Lives in this package so that a theme, or the plugin that
        /// carries it, can register itself without referencing the rest of Kameleon. Registering a theme does
        /// not make it available to any tenant; that is an administrator's decision.
        /// </summary>
        public IServiceCollection AddKameleonTheme<TTheme>()
            where TTheme : class, ITheme
        {
            services.TryAddEnumerable(ServiceDescriptor.Singleton<ITheme, TTheme>());
            return services;
        }
    }
}
