namespace Kameleon.Settings;

/// <summary>
/// Where theme decisions are kept. Kameleon uses its EF Core store when a <c>Kameleon.Data.*</c> provider is
/// registered and an in-memory store otherwise. A host can register its own, for example to keep the values in
/// a settings table it already has; it must then call <see cref="Resolution.IThemeChangeNotifier"/> after
/// writing outside Kameleon's services.
/// </summary>
/// <remarks>Every method takes the tenant explicitly. Deciding which tenant a caller may touch is the services' job.</remarks>
public interface IThemeSettingsStore
{
    /// <summary>
    /// Gets the catalog.
    /// </summary>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the theme catalog records.</returns>
    Task<IReadOnlyList<ThemeCatalogRecord>> GetCatalogAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves the catalog.
    /// </summary>
    /// <param name="record">The record.</param>
    /// <param name="updatedBy">The updated by.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task SaveCatalogAsync(ThemeCatalogRecord record, string? updatedBy, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the tenant settings.
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the tenant theme settings record.</returns>
    Task<TenantThemeSettingsRecord?> GetTenantSettingsAsync(string tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves the tenant settings.
    /// </summary>
    /// <param name="record">The record.</param>
    /// <param name="updatedBy">The updated by.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task SaveTenantSettingsAsync(TenantThemeSettingsRecord record, string? updatedBy, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the tenant themes.
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the tenant theme records.</returns>
    Task<IReadOnlyList<TenantThemeRecord>> GetTenantThemesAsync(string tenantId, CancellationToken cancellationToken = default);

    /// <summary>Every tenant's record for one theme, for granting it to tenants.</summary>
    Task<IReadOnlyList<TenantThemeRecord>> GetThemeTenantsAsync(string themeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves the tenant theme.
    /// </summary>
    /// <param name="record">The record.</param>
    /// <param name="updatedBy">The updated by.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task SaveTenantThemeAsync(TenantThemeRecord record, string? updatedBy, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the user theme.
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="userId">The user identifier.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the string.</returns>
    Task<string?> GetUserThemeAsync(string tenantId, string userId, CancellationToken cancellationToken = default);

    /// <summary>Null removes the user's choice, so they follow the tenant default again.</summary>
    Task SetUserThemeAsync(string tenantId, string userId, string? themeId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents the theme catalog record.
/// </summary>
/// <param name="ThemeId">The theme identifier.</param>
/// <param name="IsEnabled">The is enabled.</param>
/// <param name="Availability">The availability.</param>
public sealed record ThemeCatalogRecord(string ThemeId, bool IsEnabled, ThemeAvailability Availability);

/// <summary>
/// Represents the tenant theme settings record.
/// </summary>
/// <param name="TenantId">The tenant identifier.</param>
/// <param name="DefaultThemeId">The default theme identifier.</param>
/// <param name="DefaultLayoutId">The default layout identifier.</param>
/// <param name="AllowUserOverride">The allow user override.</param>
public sealed record TenantThemeSettingsRecord(string TenantId, string? DefaultThemeId, string? DefaultLayoutId, bool AllowUserOverride);

/// <summary>
/// Represents the tenant theme record.
/// </summary>
/// <param name="TenantId">The tenant identifier.</param>
/// <param name="ThemeId">The theme identifier.</param>
/// <param name="IsGranted">The is granted.</param>
/// <param name="IsEnabled">The is enabled.</param>
/// <param name="Configuration">The configuration.</param>
public sealed record TenantThemeRecord(
    string TenantId,
    string ThemeId,
    bool IsGranted,
    bool IsEnabled,
    IReadOnlyDictionary<string, string> Configuration);
