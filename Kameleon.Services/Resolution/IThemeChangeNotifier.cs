namespace Kameleon.Resolution;

/// <summary>
/// Clears cached resolutions and tells open Blazor circuits to resolve their theme again. Kameleon's own
/// services call it after every change; call it yourself after changing theme settings any other way.
/// </summary>
public interface IThemeChangeNotifier
{
    /// <summary>Raised after the cache is cleared. It can fire on any thread.</summary>
    event EventHandler<ThemeChangedEventArgs>? Changed;

    /// <summary>
    /// Performs the tenant changed operation.
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    void TenantChanged(string tenantId);

    /// <summary>
    /// Performs the user changed operation.
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="userId">The user identifier.</param>
    void UserChanged(string tenantId, string userId);

    /// <summary>For changes that can affect every tenant, such as disabling a theme or reloading the catalog.</summary>
    void AllChanged();
}

/// <param name="TenantId">Null when every tenant is affected.</param>
/// <param name="UserId">Set when only one user's choice changed.</param>
public sealed class ThemeChangedEventArgs(string? tenantId, string? userId) : EventArgs
{
    /// <summary>
    /// Gets the tenant identifier.
    /// </summary>
    public string? TenantId { get; } = tenantId;

    /// <summary>
    /// Gets the user identifier.
    /// </summary>
    public string? UserId { get; } = userId;

    /// <summary>Whether a circuit showing this tenant and user needs to resolve again.</summary>
    public bool Affects(string tenantId, string? userId) =>
        TenantId is null
        || (string.Equals(TenantId, tenantId, StringComparison.Ordinal)
            && (UserId is null || string.Equals(UserId, userId, StringComparison.Ordinal)));
}
