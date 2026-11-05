using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

namespace Kameleon.Infrastructure;

/// <summary>
/// The signed-in user of the current request or circuit. Reads the request during prerendering and the
/// circuit's authentication state afterwards, because an interactive circuit has no <c>HttpContext</c>.
/// </summary>
internal sealed class CurrentPrincipal(IHttpContextAccessor httpContextAccessor, IServiceProvider services)
{
    private static readonly ClaimsPrincipal Anonymous = new(new ClaimsIdentity());

    public async Task<ClaimsPrincipal> GetAsync()
    {
        var httpUser = httpContextAccessor.HttpContext?.User;
        if (httpUser?.Identity?.IsAuthenticated == true)
        {
            return httpUser;
        }

        if (services.GetService<AuthenticationStateProvider>() is not { } stateProvider)
        {
            return httpUser ?? Anonymous;
        }

        try
        {
            var state = await stateProvider.GetAuthenticationStateAsync();
            return state.User;
        }
        catch (InvalidOperationException)
        {
            // Outside a circuit the server provider has no state to give.
            return httpUser ?? Anonymous;
        }
    }

    /// <summary>For synchronous callers. On an established circuit the authentication task has already completed.</summary>
    public ClaimsPrincipal? GetSignedIn()
    {
        var httpUser = httpContextAccessor.HttpContext?.User;
        if (httpUser?.Identity?.IsAuthenticated == true)
        {
            return httpUser;
        }

        if (httpContextAccessor.HttpContext is not null)
        {
            return null;
        }

        var user = Task.Run(GetAsync).GetAwaiter().GetResult();
        return user.Identity?.IsAuthenticated == true ? user : null;
    }
}
