using System.Collections.ObjectModel;

namespace Kameleon.Administration;

internal sealed class ThemeCatalogAdminService(
    ILogger<ThemeCatalogAdminService> logger,
    IThemeAuthorization authorization,
    IUserContextService users,
    IThemeCatalog catalog,
    IThemeSettingsStore store,
    ThemeSettingsCache cache,
    IEnumerable<IThemeTenantDirectory> directories,
    IOptions<KameleonOptions> options) : IThemeCatalogAdminService
{
    private IThemeTenantDirectory? Directory => directories.LastOrDefault();

    public async Task<Result<ThemeCatalogDto>> GetAsync(CancellationToken cancellationToken = default)
    {
        if (!await authorization.CanManageCatalogAsync(cancellationToken))
        {
            return Result<ThemeCatalogDto>.Forbidden();
        }

        try
        {
            var entries = (await store.GetCatalogAsync(cancellationToken)).ToDictionary(entry => entry.ThemeId, StringComparer.Ordinal);
            var themes = catalog.Themes
                .Select(theme =>
                {
                    var entry = entries.GetValueOrDefault(theme.Id);
                    return new CatalogThemeDto(
                        ThemeSummaryDto.From(theme),
                        entry?.IsEnabled ?? true,
                        entry?.Availability ?? options.Value.DefaultAvailability,
                        theme.Id == options.Value.FallbackThemeId,
                        theme.Source);
                })
                .ToList();

            return Result<ThemeCatalogDto>.Success(new ThemeCatalogDto(themes, catalog.Errors, Directory is not null));
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "The theme catalog settings could not be read");
            return Result<ThemeCatalogDto>.Error("The theme catalog could not be loaded.");
        }
    }

    public Task<Result> SetEnabledAsync(string themeId, bool enabled, CancellationToken cancellationToken = default) =>
        ChangeEntryAsync(themeId, cancellationToken, (theme, entry) =>
        {
            if (!enabled && theme.Id == options.Value.FallbackThemeId)
            {
                return Result<ThemeCatalogRecord>.Invalid(new ValidationError(nameof(themeId), $"{theme.Name} is the fallback theme and cannot be disabled."));
            }

            return entry with { IsEnabled = enabled };
        });

    public Task<Result> SetAvailabilityAsync(string themeId, ThemeAvailability availability, CancellationToken cancellationToken = default) =>
        ChangeEntryAsync(themeId, cancellationToken, (_, entry) =>
            Enum.IsDefined(availability)
                ? entry with { Availability = availability }
                : Result<ThemeCatalogRecord>.Invalid(new ValidationError(nameof(availability), "Unknown availability.")));

    public async Task<Result<IReadOnlyList<ThemeTenantGrantDto>>> GetGrantsAsync(string themeId, CancellationToken cancellationToken = default)
    {
        if (!await authorization.CanManageCatalogAsync(cancellationToken))
        {
            return Result<IReadOnlyList<ThemeTenantGrantDto>>.Forbidden();
        }

        if (catalog.Find(themeId) is null)
        {
            return Result<IReadOnlyList<ThemeTenantGrantDto>>.NotFound($"Theme {themeId} is not installed.");
        }

        if (Directory is not { } directory)
        {
            return Result<IReadOnlyList<ThemeTenantGrantDto>>.Success([]);
        }

        try
        {
            var granted = (await store.GetThemeTenantsAsync(themeId, cancellationToken))
                .Where(record => record.IsGranted)
                .Select(record => record.TenantId)
                .ToHashSet(StringComparer.Ordinal);

            var tenants = await directory.ListAsync(cancellationToken);
            IReadOnlyList<ThemeTenantGrantDto> result =
            [
                .. tenants
                    .OrderBy(tenant => tenant.Name, StringComparer.OrdinalIgnoreCase)
                    .Select(tenant => new ThemeTenantGrantDto(tenant.Id, tenant.Name, granted.Contains(tenant.Id))),
            ];

            return Result<IReadOnlyList<ThemeTenantGrantDto>>.Success(result);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "The tenants granted theme {ThemeId} could not be read", themeId);
            return Result<IReadOnlyList<ThemeTenantGrantDto>>.Error("The tenants could not be loaded.");
        }
    }

    public async Task<Result> SetGrantAsync(string themeId, string tenantId, bool granted, CancellationToken cancellationToken = default)
    {
        if (!await authorization.CanManageCatalogAsync(cancellationToken))
        {
            return Result.Forbidden();
        }

        if (catalog.Find(themeId) is null)
        {
            return Result.NotFound($"Theme {themeId} is not installed.");
        }

        string? userId = users.GetCurrentUserId();
        try
        {
            if (Directory is { } directory && (await directory.ListAsync(cancellationToken)).All(tenant => tenant.Id != tenantId))
            {
                return Result.NotFound($"Tenant {tenantId} was not found.");
            }

            var snapshot = await TenantThemeSnapshot.LoadAsync(store, tenantId, cancellationToken);
            var record = snapshot.Themes.GetValueOrDefault(themeId)
                ?? new TenantThemeRecord(
                    tenantId,
                    themeId,
                    IsGranted: false,
                    IsEnabled: ThemeEligibility.IsEnabledByTenant(themeId, snapshot, options.Value),
                    ReadOnlyDictionary<string, string>.Empty);

            await store.SaveTenantThemeAsync(record with { IsGranted = granted }, userId, cancellationToken);
            cache.TenantChanged(tenantId);

            logger.LogInformation("User {UserId} {Action} theme {ThemeId} for tenant {TenantId}", userId, granted ? "granted" : "revoked", themeId, tenantId);
            return Result.Success();
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "User {UserId} could not change the grant of theme {ThemeId} for tenant {TenantId}", userId, themeId, tenantId);
            return Result.Error("The change could not be saved.");
        }
    }

    public async Task<Result> ReloadAsync(CancellationToken cancellationToken = default)
    {
        if (!await authorization.CanManageCatalogAsync(cancellationToken))
        {
            return Result.Forbidden();
        }

        catalog.Reload();
        cache.AllChanged();

        logger.LogInformation("User {UserId} reloaded the theme catalog", users.GetCurrentUserId());
        return Result.Success();
    }

    private async Task<Result> ChangeEntryAsync(
        string themeId,
        CancellationToken cancellationToken,
        Func<ThemeInfo, ThemeCatalogRecord, Result<ThemeCatalogRecord>> change)
    {
        if (!await authorization.CanManageCatalogAsync(cancellationToken))
        {
            return Result.Forbidden();
        }

        if (catalog.Find(themeId) is not { } theme)
        {
            return Result.NotFound($"Theme {themeId} is not installed.");
        }

        string? userId = users.GetCurrentUserId();
        try
        {
            var current = (await store.GetCatalogAsync(cancellationToken)).FirstOrDefault(entry => entry.ThemeId == themeId)
                ?? new ThemeCatalogRecord(themeId, true, options.Value.DefaultAvailability);

            var changed = change(theme, current);
            if (!changed.IsSuccess)
            {
                return Result.Invalid(changed.ValidationErrors);
            }

            await store.SaveCatalogAsync(changed.Value, userId, cancellationToken);
            cache.AllChanged();

            logger.LogInformation(
                "User {UserId} changed theme {ThemeId}: enabled {IsEnabled}, availability {Availability}",
                userId, themeId, changed.Value.IsEnabled, changed.Value.Availability);
            return Result.Success();
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "User {UserId} could not change theme {ThemeId}", userId, themeId);
            return Result.Error("The change could not be saved.");
        }
    }
}
