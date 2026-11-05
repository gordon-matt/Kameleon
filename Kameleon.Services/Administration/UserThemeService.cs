namespace Kameleon.Administration;

internal sealed class UserThemeService(
    ILogger<UserThemeService> logger,
    IUserContextService users,
    IThemeTenantProvider tenants,
    IThemeCatalog catalog,
    IThemeSettingsStore store,
    ThemeSettingsCache cache,
    IOptions<KameleonOptions> options) : IUserThemeService
{
    public async Task<Result<UserThemeChoicesDto>> GetChoicesAsync(CancellationToken cancellationToken = default)
    {
        string tenantId = tenants.GetTenantId() ?? string.Empty;
        string? userId = users.GetCurrentUserId();
        try
        {
            var snapshot = await cache.GetSnapshotAsync(tenantId, store, cancellationToken);
            var settings = options.Value;
            var eligible = ThemeEligibility.Eligible(catalog, snapshot, settings);
            string? selected = userId is null ? null : await cache.GetUserThemeAsync(tenantId, userId, store, cancellationToken);
            if (selected is not null && eligible.All(theme => theme.Id != selected))
            {
                selected = null;
            }

            bool canChoose = userId is not null && ThemeEligibility.AllowUserOverride(snapshot, settings) && eligible.Count > 1;
            return Result<UserThemeChoicesDto>.Success(new UserThemeChoicesDto(
                canChoose,
                selected,
                ThemeEligibility.Default(snapshot, settings).ThemeId,
                [.. eligible.Select(ThemeSummaryDto.From)]));
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "The theme choices of user {UserId} in tenant {TenantId} could not be read", userId, tenantId);
            return Result<UserThemeChoicesDto>.Error("The themes could not be loaded.");
        }
    }

    public async Task<Result> SetAsync(string? themeId, CancellationToken cancellationToken = default)
    {
        if (users.GetCurrentUserId() is not string userId)
        {
            return Result.Forbidden();
        }

        string tenantId = tenants.GetTenantId() ?? string.Empty;
        try
        {
            var snapshot = await TenantThemeSnapshot.LoadAsync(store, tenantId, cancellationToken);
            var settings = options.Value;
            if (!ThemeEligibility.AllowUserOverride(snapshot, settings))
            {
                return Result.Forbidden();
            }

            if (themeId is not null
                && (catalog.Find(themeId) is not { } theme || !ThemeEligibility.IsEligible(theme, snapshot, settings)))
            {
                return Result.Invalid(new ValidationError(nameof(themeId), "That theme is not offered on this site."));
            }

            await store.SetUserThemeAsync(tenantId, userId, themeId, cancellationToken);
            cache.UserChanged(tenantId, userId);

            logger.LogInformation("User {UserId} chose theme {ThemeId} in tenant {TenantId}", userId, themeId, tenantId);
            return Result.Success();
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "User {UserId} could not change their theme in tenant {TenantId}", userId, tenantId);
            return Result.Error("Your theme could not be saved.");
        }
    }
}
