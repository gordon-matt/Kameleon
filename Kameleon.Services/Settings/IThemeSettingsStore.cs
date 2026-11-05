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
    Task<IReadOnlyList<ThemeCatalogRecord>> GetCatalogAsync(CancellationToken cancellationToken = default);

    Task SaveCatalogAsync(ThemeCatalogRecord record, string? updatedBy, CancellationToken cancellationToken = default);

    Task<TenantThemeSettingsRecord?> GetTenantSettingsAsync(string tenantId, CancellationToken cancellationToken = default);

    Task SaveTenantSettingsAsync(TenantThemeSettingsRecord record, string? updatedBy, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TenantThemeRecord>> GetTenantThemesAsync(string tenantId, CancellationToken cancellationToken = default);

    /// <summary>Every tenant's record for one theme, for granting it to tenants.</summary>
    Task<IReadOnlyList<TenantThemeRecord>> GetThemeTenantsAsync(string themeId, CancellationToken cancellationToken = default);

    Task SaveTenantThemeAsync(TenantThemeRecord record, string? updatedBy, CancellationToken cancellationToken = default);

    Task<string?> GetUserThemeAsync(string tenantId, string userId, CancellationToken cancellationToken = default);

    /// <summary>Null removes the user's choice, so they follow the tenant default again.</summary>
    Task SetUserThemeAsync(string tenantId, string userId, string? themeId, CancellationToken cancellationToken = default);
}

public sealed record ThemeCatalogRecord(string ThemeId, bool IsEnabled, ThemeAvailability Availability);

public sealed record TenantThemeSettingsRecord(string TenantId, string? DefaultThemeId, string? DefaultLayoutId, bool AllowUserOverride);

public sealed record TenantThemeRecord(
    string TenantId,
    string ThemeId,
    bool IsGranted,
    bool IsEnabled,
    IReadOnlyDictionary<string, string> Configuration);
