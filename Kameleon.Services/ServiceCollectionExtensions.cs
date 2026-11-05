using Kameleon.FileSystem;
using Kameleon.Hosting;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Kameleon;

public static class KameleonServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers Kameleon's services without any UI. Settings are kept in memory unless a provider from one
        /// of the <c>Kameleon.Data.*</c> packages is added. Most hosts call <c>AddKameleon</c> from the
        /// <c>Kameleon</c> package instead, which calls this.
        /// </summary>
        public IServiceCollection AddKameleonServices(Action<KameleonOptions>? configure = null)
        {
            services.AddOptions<KameleonOptions>()
                .Configure(options => configure?.Invoke(options))
                .PostConfigure(options =>
                {
                    options.AssetRequestPath = NormalizePath(options.AssetRequestPath, KameleonOptions.DefaultAssetRequestPath);
                    options.AdminPath = NormalizePath(options.AdminPath, KameleonOptions.DefaultAdminPath);
                })
                .Validate(
                    options => IsValidOrNull(options.FallbackThemeId) && IsValidOrNull(options.DefaultThemeId) && options.EnabledThemes.All(ThemeIds.IsValid),
                    "Kameleon's FallbackThemeId, DefaultThemeId and EnabledThemes must be theme ids: lowercase letters, digits and single hyphens.")
                .Validate(
                    options => IsLayoutOrNull(options.HostLayout) && IsLayoutOrNull(options.AdminLayout),
                    "Kameleon's HostLayout and AdminLayout must be layout components.")
                .ValidateOnStart();

            services.AddMemoryCache();
            services.TryAddSingleton(TimeProvider.System);
            services.TryAddScoped<IThemeTenantProvider, SingleTenantThemeTenantProvider>();
            services.TryAddScoped<IThemeAuthorization, DenyThemeAuthorization>();

            services.TryAddEnumerable(ServiceDescriptor.Singleton<IThemeSource, CompiledThemeSource>());
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IThemeSource, FileSystemThemeSource>());
            services.TryAddSingleton<IThemeCatalog, ThemeCatalog>();

            services.TryAddSingleton<ThemeSettingsCache>();
            services.TryAddSingleton<IThemeChangeNotifier>(provider => provider.GetRequiredService<ThemeSettingsCache>());

            // Closed registrations against Kameleon's own factory. A host built on Extenso maps the open
            // IRepository<> to its own IDbContextFactory, and closed registrations win, so the two never cross.
            services.TryAddScoped(KameleonRepository<ThemeCatalogEntry>);
            services.TryAddScoped(KameleonRepository<TenantThemeSettings>);
            services.TryAddScoped(KameleonRepository<TenantTheme>);
            services.TryAddScoped(KameleonRepository<UserThemePreference>);

            services.TryAddSingleton<InMemoryThemeSettingsStore>();
            services.TryAddScoped<EntityFrameworkThemeSettingsStore>();
            services.TryAddScoped<IThemeSettingsStore>(provider =>
                provider.GetService<IKameleonDbContextFactory>() is null
                    ? provider.GetRequiredService<InMemoryThemeSettingsStore>()
                    : provider.GetRequiredService<EntityFrameworkThemeSettingsStore>());

            services.TryAddScoped<IThemeResolver, ThemeResolver>();
            services.TryAddScoped<IThemeAdminService, ThemeAdminService>();
            services.TryAddScoped<IThemeCatalogAdminService, ThemeCatalogAdminService>();
            services.TryAddScoped<IUserThemeService, UserThemeService>();

            services.AddHostedService<KameleonDatabaseInitializer>();

            return services;
        }
    }

    private static bool IsValidOrNull(string? themeId) => themeId is null || ThemeIds.IsValid(themeId);

    private static bool IsLayoutOrNull(Type? type) =>
        type is null || typeof(Microsoft.AspNetCore.Components.LayoutComponentBase).IsAssignableFrom(type);

    private static string NormalizePath(string? path, string fallback)
    {
        string trimmed = path?.Trim().Trim('/') ?? string.Empty;
        return trimmed.Length == 0 ? fallback : $"/{trimmed}";
    }

    private static IRepository<TEntity> KameleonRepository<TEntity>(IServiceProvider provider)
        where TEntity : class, IEntity =>
        new EntityFrameworkRepository<TEntity>(provider.GetRequiredService<IKameleonDbContextFactory>());
}
