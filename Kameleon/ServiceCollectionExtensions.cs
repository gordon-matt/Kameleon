using Heroicsoft.NetForge.Abstractions.Auth;
using Heroicsoft.NetForge.Components;
using Kameleon.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Kameleon;

public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers the theme engine, its screens and the <see cref="KameleonPolicies"/>. Settings are kept in
        /// memory unless a storage provider is added (<c>AddKameleonSqlite</c>, <c>AddKameleonSqlServer</c>,
        /// <c>AddKameleonNpgsql</c> or <c>AddKameleonMySql</c>). Register themes with <c>AddKameleonTheme</c>,
        /// use <see cref="KameleonLayout"/> as the layout of themed pages and call <c>app.MapKameleon()</c>.
        /// </summary>
        public IServiceCollection AddKameleon(Action<KameleonOptions>? configure = null)
        {
            // Before the services, which otherwise register an authorization that refuses everything.
            services.TryAddScoped<IThemeAuthorization, PolicyThemeAuthorization>();
            services.AddKameleonServices(configure);

            // Safe alongside a host that is itself a Blazor app: these calls are idempotent.
            services.AddRazorComponents().AddInteractiveServerComponents();
            services.AddCascadingAuthenticationState();
            services.AddHttpContextAccessor();

            // The host's own ForgeUiOptions win; this only fills them in when the host has none.
            services.AddNetForgeComponents();
            services.AddNetForgeMenuContributor<KameleonMenuContributor>();

            services.TryAddScoped<CurrentPrincipal>();
            services.TryAddScoped<IUserContextService, ClaimsUserContextService>();
            services.TryAddScoped<ThemeState>();

            services.AddAuthorization();
            services.AddOptions<AuthorizationOptions>()
                .Configure<IOptions<KameleonOptions>>((authorization, kameleon) =>
                {
                    AddRolePolicy(authorization, KameleonPolicies.Manage, kameleon.Value.ManagerRoles);
                    AddRolePolicy(authorization, KameleonPolicies.View, kameleon.Value.ManagerRoles);
                    AddRolePolicy(authorization, KameleonPolicies.ManageCatalog, kameleon.Value.CatalogManagerRoles);
                });

            return services;
        }
    }

    /// <summary>Leaves a policy the host already registered under this name alone.</summary>
    private static void AddRolePolicy(AuthorizationOptions authorization, string name, IList<string> roles)
    {
        if (authorization.GetPolicy(name) is not null)
        {
            return;
        }

        authorization.AddPolicy(name, policy =>
        {
            policy.RequireAuthenticatedUser();
            if (roles.Count > 0)
            {
                policy.RequireRole(roles);
            }
        });
    }
}
