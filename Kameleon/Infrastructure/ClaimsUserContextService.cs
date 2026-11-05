using System.Security.Claims;
using Heroicsoft.NetForge.Abstractions.Auth;

namespace Kameleon.Infrastructure;

/// <summary>Used only when the host has not registered an <see cref="IUserContextService"/>.</summary>
internal sealed class ClaimsUserContextService(CurrentPrincipal principal) : IUserContextService
{
    private const string AdministratorRole = "Administrator";

    private static readonly string[] RoleClaimTypes = [ClaimTypes.Role, "role", "roles"];

    public string? GetCurrentUserId()
    {
        var user = principal.GetSignedIn();
        return user?.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? user?.FindFirst("sub")?.Value
            ?? user?.Identity?.Name;
    }

    public string? GetCurrentUserName()
    {
        var user = principal.GetSignedIn();
        return user?.Identity?.Name ?? user?.FindFirst("name")?.Value;
    }

    public bool IsAuthenticated() => principal.GetSignedIn() is not null;

    public bool IsAdministrator() =>
        GetRoleNames().Contains(AdministratorRole, StringComparer.OrdinalIgnoreCase);

    public IReadOnlyList<string> GetRoleNames()
    {
        var user = principal.GetSignedIn();
        if (user is null)
        {
            return [];
        }

        var types = new HashSet<string>(RoleClaimTypes, StringComparer.Ordinal);
        if (user.Identity is ClaimsIdentity identity)
        {
            types.Add(identity.RoleClaimType);
        }

        return [.. user.Claims
            .Where(claim => types.Contains(claim.Type))
            .Select(claim => claim.Value)
            .Distinct(StringComparer.OrdinalIgnoreCase)];
    }
}
