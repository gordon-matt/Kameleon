namespace Kameleon.Resolution;

internal sealed class ThemeResolver(
    ILogger<ThemeResolver> logger,
    IThemeCatalog catalog,
    IThemeSettingsStore store,
    ThemeSettingsCache cache,
    IThemeTenantProvider tenants,
    IUserContextService users,
    IOptions<KameleonOptions> options) : IThemeResolver
{
    public async Task<ThemeContext> ResolveAsync(CancellationToken cancellationToken = default)
    {
        string tenantId = tenants.GetTenantId() ?? string.Empty;
        string? userId = users.GetCurrentUserId();
        var settings = options.Value;

        TenantThemeSnapshot snapshot;
        try
        {
            snapshot = await cache.GetSnapshotAsync(tenantId, store, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "The theme settings of tenant {TenantId} could not be read; the fallback theme is used", tenantId);
            return Build(catalog.Find(settings.FallbackThemeId), tenantId, userId, null, false, null);
        }

        if (userId is not null && ThemeEligibility.AllowUserOverride(snapshot, settings))
        {
            string? chosen = await GetUserThemeAsync(tenantId, userId, cancellationToken);
            if (catalog.Find(chosen) is { } userTheme && ThemeEligibility.IsEligible(userTheme, snapshot, settings))
            {
                return Build(userTheme, tenantId, userId, null, true, snapshot);
            }
        }

        var (defaultThemeId, defaultLayoutId) = ThemeEligibility.Default(snapshot, settings);
        if (catalog.Find(defaultThemeId) is { } defaultTheme && ThemeEligibility.IsEligible(defaultTheme, snapshot, settings))
        {
            return Build(defaultTheme, tenantId, userId, defaultLayoutId, false, snapshot);
        }

        return Build(catalog.Find(settings.FallbackThemeId), tenantId, userId, null, false, snapshot);
    }

    private async Task<string?> GetUserThemeAsync(string tenantId, string userId, CancellationToken cancellationToken)
    {
        try
        {
            return await cache.GetUserThemeAsync(tenantId, userId, store, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "The theme choice of user {UserId} in tenant {TenantId} could not be read; the tenant default is used", userId, tenantId);
            return null;
        }
    }

    private static ThemeContext Build(
        ThemeInfo? theme,
        string tenantId,
        string? userId,
        string? layoutId,
        bool isUserOverride,
        TenantThemeSnapshot? snapshot)
    {
        if (theme is null)
        {
            return ThemeContext.None(tenantId, userId);
        }

        var saved = snapshot?.Themes.GetValueOrDefault(theme.Id)?.Configuration;
        return new ThemeContext
        {
            TenantId = tenantId,
            UserId = userId,
            Theme = theme,
            Layout = theme.FindLayout(layoutId) ?? theme.DefaultLayout,
            IsUserOverride = isUserOverride,
            Settings = ThemeSettingValues.Merge(theme.Settings, saved),
        };
    }
}
