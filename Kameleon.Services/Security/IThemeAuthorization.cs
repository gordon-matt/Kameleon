namespace Kameleon.Security;

/// <summary>
/// The permission checks Kameleon's services make before reading or changing settings, so the rules hold even
/// when a screen forgets to check. The <c>Kameleon</c> package implements this with authorization policies; a
/// host that uses only <c>Kameleon.Services</c> must register its own.
/// </summary>
public interface IThemeAuthorization
{
    /// <summary>May see the current tenant's theme settings.</summary>
    Task<bool> CanViewAsync(CancellationToken cancellationToken = default);

    /// <summary>May change the current tenant's theme settings.</summary>
    Task<bool> CanManageAsync(CancellationToken cancellationToken = default);

    /// <summary>May change application-wide settings and grant themes to any tenant.</summary>
    Task<bool> CanManageCatalogAsync(CancellationToken cancellationToken = default);
}

/// <summary>Registered when nothing else is, so that a missing registration fails closed.</summary>
internal sealed class DenyThemeAuthorization : IThemeAuthorization
{
    public Task<bool> CanViewAsync(CancellationToken cancellationToken = default) => Task.FromResult(false);

    public Task<bool> CanManageAsync(CancellationToken cancellationToken = default) => Task.FromResult(false);

    public Task<bool> CanManageCatalogAsync(CancellationToken cancellationToken = default) => Task.FromResult(false);
}
