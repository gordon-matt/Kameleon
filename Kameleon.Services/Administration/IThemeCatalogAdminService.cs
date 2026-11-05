namespace Kameleon.Administration;

/// <summary>
/// Application-wide theme decisions: which installed themes may be used, and by which tenants. Installing and
/// removing compiled themes belongs to whatever installs the host's packages or plugins, not to Kameleon.
/// </summary>
public interface IThemeCatalogAdminService
{
    Task<Result<ThemeCatalogDto>> GetAsync(CancellationToken cancellationToken = default);

    /// <summary>Disabling takes the theme away from every tenant at once. The fallback theme cannot be disabled.</summary>
    Task<Result> SetEnabledAsync(string themeId, bool enabled, CancellationToken cancellationToken = default);

    Task<Result> SetAvailabilityAsync(string themeId, ThemeAvailability availability, CancellationToken cancellationToken = default);

    /// <summary>Every tenant from the host's <see cref="IThemeTenantDirectory"/> and whether it was granted the theme.</summary>
    Task<Result<IReadOnlyList<ThemeTenantGrantDto>>> GetGrantsAsync(string themeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Grants or revokes a theme for one tenant. Only matters while the theme is available to selected tenants.
    /// A tenant that loses its default theme falls back without its saved default being rewritten.
    /// </summary>
    Task<Result> SetGrantAsync(string themeId, string tenantId, bool granted, CancellationToken cancellationToken = default);

    /// <summary>Reads static theme folders again.</summary>
    Task<Result> ReloadAsync(CancellationToken cancellationToken = default);
}
