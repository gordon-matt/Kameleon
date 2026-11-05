using Kameleon.Security;
using Microsoft.AspNetCore.Authorization;

namespace Kameleon.Infrastructure;

/// <summary>Checks the <see cref="KameleonPolicies"/> against the current user, so the services enforce what the screens show.</summary>
internal sealed class PolicyThemeAuthorization(IAuthorizationService authorization, CurrentPrincipal principal) : IThemeAuthorization
{
    public Task<bool> CanViewAsync(CancellationToken cancellationToken = default) => IsAllowedAsync(KameleonPolicies.View);

    public Task<bool> CanManageAsync(CancellationToken cancellationToken = default) => IsAllowedAsync(KameleonPolicies.Manage);

    public Task<bool> CanManageCatalogAsync(CancellationToken cancellationToken = default) => IsAllowedAsync(KameleonPolicies.ManageCatalog);

    private async Task<bool> IsAllowedAsync(string policy)
    {
        var user = await principal.GetAsync();
        var result = await authorization.AuthorizeAsync(user, policy);
        return result.Succeeded;
    }
}
