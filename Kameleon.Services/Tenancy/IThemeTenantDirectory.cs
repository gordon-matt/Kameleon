namespace Kameleon.Tenancy;

/// <summary>
/// Lists the host's tenants, so an application administrator can grant a theme to some of them. Optional:
/// without it, themes can still be made available to all tenants or to none.
/// </summary>
public interface IThemeTenantDirectory
{
    /// <summary>
    /// Lists the value.
    /// </summary>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the theme tenants.</returns>
    Task<IReadOnlyList<ThemeTenant>> ListAsync(CancellationToken cancellationToken = default);
}

/// <param name="Id">The same key <see cref="IThemeTenantProvider.GetTenantId"/> returns for that tenant.</param>
public sealed record ThemeTenant(string Id, string Name);
