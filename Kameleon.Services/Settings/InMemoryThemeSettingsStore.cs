namespace Kameleon.Settings;

/// <summary>
/// Used when no database provider is registered, so a single site or a demo works with configuration alone.
/// Changes made on the admin screens last until the application restarts.
/// </summary>
internal sealed class InMemoryThemeSettingsStore : IThemeSettingsStore
{
    private readonly Lock sync = new();
    private readonly Dictionary<string, ThemeCatalogRecord> catalog = new(StringComparer.Ordinal);
    private readonly Dictionary<string, TenantThemeSettingsRecord> tenantSettings = new(StringComparer.Ordinal);
    private readonly Dictionary<(string TenantId, string ThemeId), TenantThemeRecord> tenantThemes = [];
    private readonly Dictionary<(string TenantId, string UserId), string> userThemes = [];

    public Task<IReadOnlyList<ThemeCatalogRecord>> GetCatalogAsync(CancellationToken cancellationToken = default)
    {
        lock (sync)
        {
            return Task.FromResult<IReadOnlyList<ThemeCatalogRecord>>([.. catalog.Values]);
        }
    }

    public Task SaveCatalogAsync(ThemeCatalogRecord record, string? updatedBy, CancellationToken cancellationToken = default)
    {
        lock (sync)
        {
            catalog[record.ThemeId] = record;
        }

        return Task.CompletedTask;
    }

    public Task<TenantThemeSettingsRecord?> GetTenantSettingsAsync(string tenantId, CancellationToken cancellationToken = default)
    {
        lock (sync)
        {
            return Task.FromResult(tenantSettings.GetValueOrDefault(tenantId));
        }
    }

    public Task SaveTenantSettingsAsync(TenantThemeSettingsRecord record, string? updatedBy, CancellationToken cancellationToken = default)
    {
        lock (sync)
        {
            tenantSettings[record.TenantId] = record;
        }

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<TenantThemeRecord>> GetTenantThemesAsync(string tenantId, CancellationToken cancellationToken = default)
    {
        lock (sync)
        {
            return Task.FromResult<IReadOnlyList<TenantThemeRecord>>([.. tenantThemes.Values.Where(record => record.TenantId == tenantId)]);
        }
    }

    public Task<IReadOnlyList<TenantThemeRecord>> GetThemeTenantsAsync(string themeId, CancellationToken cancellationToken = default)
    {
        lock (sync)
        {
            return Task.FromResult<IReadOnlyList<TenantThemeRecord>>([.. tenantThemes.Values.Where(record => record.ThemeId == themeId)]);
        }
    }

    public Task SaveTenantThemeAsync(TenantThemeRecord record, string? updatedBy, CancellationToken cancellationToken = default)
    {
        lock (sync)
        {
            tenantThemes[(record.TenantId, record.ThemeId)] = record;
        }

        return Task.CompletedTask;
    }

    public Task<string?> GetUserThemeAsync(string tenantId, string userId, CancellationToken cancellationToken = default)
    {
        lock (sync)
        {
            return Task.FromResult(userThemes.GetValueOrDefault((tenantId, userId)));
        }
    }

    public Task SetUserThemeAsync(string tenantId, string userId, string? themeId, CancellationToken cancellationToken = default)
    {
        lock (sync)
        {
            if (themeId is null)
            {
                userThemes.Remove((tenantId, userId));
            }
            else
            {
                userThemes[(tenantId, userId)] = themeId;
            }
        }

        return Task.CompletedTask;
    }
}
