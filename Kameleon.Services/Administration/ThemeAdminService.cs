using System.Collections.ObjectModel;

namespace Kameleon.Administration;

internal sealed class ThemeAdminService(
    ILogger<ThemeAdminService> logger,
    IThemeAuthorization authorization,
    IUserContextService users,
    IThemeTenantProvider tenants,
    IThemeCatalog catalog,
    IThemeSettingsStore store,
    ThemeSettingsCache cache,
    IOptions<KameleonOptions> options) : IThemeAdminService
{
    public async Task<Result<TenantThemesDto>> GetAsync(CancellationToken cancellationToken = default)
    {
        if (!await authorization.CanViewAsync(cancellationToken))
        {
            return Result<TenantThemesDto>.Forbidden();
        }

        string tenantId = TenantId();
        try
        {
            // Read past the cache, so an administrator always sees what is stored.
            var snapshot = await TenantThemeSnapshot.LoadAsync(store, tenantId, cancellationToken);
            var settings = options.Value;
            var (defaultThemeId, defaultLayoutId) = ThemeEligibility.Default(snapshot, settings);

            var themes = catalog.Themes
                .Where(theme => ThemeEligibility.IsGranted(theme, snapshot, settings))
                .Select(theme => new TenantThemeDto(
                    ThemeSummaryDto.From(theme),
                    ThemeEligibility.IsEnabledByTenant(theme.Id, snapshot, settings) || theme.Id == defaultThemeId,
                    theme.Id == defaultThemeId,
                    theme.Settings,
                    ThemeSettingValues.Merge(theme.Settings, snapshot.Themes.GetValueOrDefault(theme.Id)?.Configuration)))
                .OrderByDescending(theme => theme.IsDefault)
                .ThenBy(theme => theme.Theme.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();

            bool defaultIsUnavailable = defaultThemeId is not null && themes.All(theme => theme.Theme.Id != defaultThemeId);

            return Result<TenantThemesDto>.Success(new TenantThemesDto(
                defaultThemeId,
                defaultLayoutId,
                ThemeEligibility.AllowUserOverride(snapshot, settings),
                defaultIsUnavailable,
                settings.FallbackThemeId,
                themes));
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "The theme settings of tenant {TenantId} could not be read", tenantId);
            return Result<TenantThemesDto>.Error("The theme settings could not be loaded.");
        }
    }

    public Task<Result> SetDefaultAsync(SetDefaultThemeRequest request, CancellationToken cancellationToken = default) =>
        ChangeAsync(request.ThemeId, cancellationToken, async (tenantId, theme, snapshot, userId) =>
        {
            if (request.LayoutId is not null && theme.FindLayout(request.LayoutId) is null)
            {
                return Result.Invalid(new ValidationError(nameof(request.LayoutId), $"{theme.Name} has no layout by that name."));
            }

            await store.SaveTenantSettingsAsync(
                new TenantThemeSettingsRecord(tenantId, theme.Id, request.LayoutId, ThemeEligibility.AllowUserOverride(snapshot, options.Value)),
                userId,
                cancellationToken);
            await store.SaveTenantThemeAsync(Current(tenantId, theme.Id, snapshot) with { IsEnabled = true }, userId, cancellationToken);

            logger.LogInformation("User {UserId} set the default theme of tenant {TenantId} to {ThemeId} with layout {LayoutId}", userId, tenantId, theme.Id, request.LayoutId);
            return Result.Success();
        });

    public Task<Result> SetEnabledAsync(string themeId, bool enabled, CancellationToken cancellationToken = default) =>
        ChangeAsync(themeId, cancellationToken, async (tenantId, theme, snapshot, userId) =>
        {
            if (!enabled && ThemeEligibility.Default(snapshot, options.Value).ThemeId == theme.Id)
            {
                return Result.Invalid(new ValidationError(nameof(themeId), $"{theme.Name} is the default theme. Choose another default before turning it off."));
            }

            await store.SaveTenantThemeAsync(Current(tenantId, theme.Id, snapshot) with { IsEnabled = enabled }, userId, cancellationToken);

            logger.LogInformation("User {UserId} {Action} theme {ThemeId} for tenant {TenantId}", userId, enabled ? "enabled" : "disabled", theme.Id, tenantId);
            return Result.Success();
        });

    public async Task<Result> SetAllowUserOverrideAsync(bool allow, CancellationToken cancellationToken = default)
    {
        if (!await authorization.CanManageAsync(cancellationToken))
        {
            return Result.Forbidden();
        }

        string tenantId = TenantId();
        string? userId = users.GetCurrentUserId();
        try
        {
            var snapshot = await TenantThemeSnapshot.LoadAsync(store, tenantId, cancellationToken);
            var (defaultThemeId, defaultLayoutId) = ThemeEligibility.Default(snapshot, options.Value);

            await store.SaveTenantSettingsAsync(new TenantThemeSettingsRecord(tenantId, defaultThemeId, defaultLayoutId, allow), userId, cancellationToken);
            cache.TenantChanged(tenantId);

            logger.LogInformation("User {UserId} {Action} user theme choice for tenant {TenantId}", userId, allow ? "allowed" : "disallowed", tenantId);
            return Result.Success();
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "User {UserId} could not change user theme choice for tenant {TenantId}", userId, tenantId);
            return Result.Error("The setting could not be saved.");
        }
    }

    public Task<Result> SaveSettingsAsync(SaveThemeSettingsRequest request, CancellationToken cancellationToken = default) =>
        ChangeAsync(request.ThemeId, cancellationToken, async (tenantId, theme, snapshot, userId) =>
        {
            var normalized = ThemeSettingValues.Normalize(theme.Settings, request.Values);
            if (!normalized.IsSuccess)
            {
                return Result.Invalid(normalized.ValidationErrors);
            }

            await store.SaveTenantThemeAsync(Current(tenantId, theme.Id, snapshot) with { Configuration = normalized.Value }, userId, cancellationToken);

            logger.LogInformation("User {UserId} saved the settings of theme {ThemeId} for tenant {TenantId}", userId, theme.Id, tenantId);
            return Result.Success();
        });

    /// <summary>
    /// Shared checks for a change to one theme: permission, a theme this tenant may use, and a cache flush after
    /// the write. The change itself runs against a fresh snapshot.
    /// </summary>
    private async Task<Result> ChangeAsync(
        string themeId,
        CancellationToken cancellationToken,
        Func<string, ThemeInfo, TenantThemeSnapshot, string?, Task<Result>> change)
    {
        if (!await authorization.CanManageAsync(cancellationToken))
        {
            return Result.Forbidden();
        }

        string tenantId = TenantId();
        string? userId = users.GetCurrentUserId();
        try
        {
            var snapshot = await TenantThemeSnapshot.LoadAsync(store, tenantId, cancellationToken);
            if (catalog.Find(themeId) is not { } theme || !ThemeEligibility.IsGranted(theme, snapshot, options.Value))
            {
                return Result.NotFound($"Theme {themeId} is not available to this site.");
            }

            var result = await change(tenantId, theme, snapshot, userId);
            if (result.IsSuccess)
            {
                cache.TenantChanged(tenantId);
            }

            return result;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "User {UserId} could not change theme {ThemeId} for tenant {TenantId}", userId, themeId, tenantId);
            return Result.Error("The theme settings could not be saved.");
        }
    }

    /// <summary>
    /// The tenant's record for a theme, or one that captures what the tenant sees today. Writing a new record
    /// must not change whether the theme is enabled as a side effect.
    /// </summary>
    private TenantThemeRecord Current(string tenantId, string themeId, TenantThemeSnapshot snapshot) =>
        snapshot.Themes.GetValueOrDefault(themeId)
        ?? new TenantThemeRecord(
            tenantId,
            themeId,
            IsGranted: false,
            IsEnabled: ThemeEligibility.IsEnabledByTenant(themeId, snapshot, options.Value),
            ReadOnlyDictionary<string, string>.Empty);

    private string TenantId() => tenants.GetTenantId() ?? string.Empty;
}
