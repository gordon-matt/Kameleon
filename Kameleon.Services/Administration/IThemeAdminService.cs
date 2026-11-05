namespace Kameleon.Administration;

/// <summary>
/// The current tenant's theme settings. The tenant always comes from <see cref="IThemeTenantProvider"/>, never
/// from the caller, so a tenant administrator cannot reach another tenant's settings.
/// </summary>
public interface IThemeAdminService
{
    /// <summary>The themes this tenant may use, whether or not it has enabled them.</summary>
    Task<Result<TenantThemesDto>> GetAsync(CancellationToken cancellationToken = default);

    /// <summary>Also enables the theme, since a default nobody can see makes no sense.</summary>
    Task<Result> SetDefaultAsync(SetDefaultThemeRequest request, CancellationToken cancellationToken = default);

    /// <summary>Whether the tenant offers the theme. The default theme cannot be turned off.</summary>
    Task<Result> SetEnabledAsync(string themeId, bool enabled, CancellationToken cancellationToken = default);

    Task<Result> SetAllowUserOverrideAsync(bool allow, CancellationToken cancellationToken = default);

    Task<Result> SaveSettingsAsync(SaveThemeSettingsRequest request, CancellationToken cancellationToken = default);
}
