using System.Collections.ObjectModel;
using System.Text.Json;

namespace Kameleon.Settings;

/// <summary>The store used when a <c>Kameleon.Data.*</c> provider is registered.</summary>
internal sealed class EntityFrameworkThemeSettingsStore(
    ILogger<EntityFrameworkThemeSettingsStore> logger,
    IRepository<ThemeCatalogEntry> catalogEntries,
    IRepository<TenantThemeSettings> tenantSettings,
    IRepository<TenantTheme> tenantThemes,
    IRepository<UserThemePreference> userPreferences,
    TimeProvider clock) : IThemeSettingsStore
{
    public async Task<IReadOnlyList<ThemeCatalogRecord>> GetCatalogAsync(CancellationToken cancellationToken = default)
    {
        var entries = await catalogEntries.FindAsync(new SearchOptions<ThemeCatalogEntry> { CancellationToken = cancellationToken });
        return [.. entries.Select(entry => new ThemeCatalogRecord(entry.ThemeId, entry.IsEnabled, entry.Availability))];
    }

    public async Task SaveCatalogAsync(ThemeCatalogRecord record, string? updatedBy, CancellationToken cancellationToken = default)
    {
        var context = ContextOptions.ForCancellationToken(cancellationToken);
        var entry = await catalogEntries.FindOneAsync(new SearchOptions<ThemeCatalogEntry>
        {
            Query = item => item.ThemeId == record.ThemeId,
            CancellationToken = cancellationToken,
        });

        if (entry is null)
        {
            await catalogEntries.InsertAsync(new ThemeCatalogEntry
            {
                ThemeId = record.ThemeId,
                IsEnabled = record.IsEnabled,
                Availability = record.Availability,
                UpdatedAtUtc = Now(),
                UpdatedBy = updatedBy,
            }, context);
            return;
        }

        entry.IsEnabled = record.IsEnabled;
        entry.Availability = record.Availability;
        entry.UpdatedAtUtc = Now();
        entry.UpdatedBy = updatedBy;
        await catalogEntries.UpdateAsync(entry, context);
    }

    public async Task<TenantThemeSettingsRecord?> GetTenantSettingsAsync(string tenantId, CancellationToken cancellationToken = default)
    {
        var settings = await tenantSettings.FindOneAsync(new SearchOptions<TenantThemeSettings>
        {
            Query = item => item.TenantId == tenantId,
            CancellationToken = cancellationToken,
        });

        return settings is null
            ? null
            : new TenantThemeSettingsRecord(settings.TenantId, settings.DefaultThemeId, settings.DefaultLayoutId, settings.AllowUserOverride);
    }

    public async Task SaveTenantSettingsAsync(TenantThemeSettingsRecord record, string? updatedBy, CancellationToken cancellationToken = default)
    {
        var context = ContextOptions.ForCancellationToken(cancellationToken);
        var settings = await tenantSettings.FindOneAsync(new SearchOptions<TenantThemeSettings>
        {
            Query = item => item.TenantId == record.TenantId,
            CancellationToken = cancellationToken,
        });

        if (settings is null)
        {
            await tenantSettings.InsertAsync(new TenantThemeSettings
            {
                TenantId = record.TenantId,
                DefaultThemeId = record.DefaultThemeId,
                DefaultLayoutId = record.DefaultLayoutId,
                AllowUserOverride = record.AllowUserOverride,
                UpdatedAtUtc = Now(),
                UpdatedBy = updatedBy,
            }, context);
            return;
        }

        settings.DefaultThemeId = record.DefaultThemeId;
        settings.DefaultLayoutId = record.DefaultLayoutId;
        settings.AllowUserOverride = record.AllowUserOverride;
        settings.UpdatedAtUtc = Now();
        settings.UpdatedBy = updatedBy;
        await tenantSettings.UpdateAsync(settings, context);
    }

    public async Task<IReadOnlyList<TenantThemeRecord>> GetTenantThemesAsync(string tenantId, CancellationToken cancellationToken = default)
    {
        var rows = await tenantThemes.FindAsync(new SearchOptions<TenantTheme>
        {
            Query = item => item.TenantId == tenantId,
            CancellationToken = cancellationToken,
        });

        return [.. rows.Select(ToRecord)];
    }

    public async Task<IReadOnlyList<TenantThemeRecord>> GetThemeTenantsAsync(string themeId, CancellationToken cancellationToken = default)
    {
        var rows = await tenantThemes.FindAsync(new SearchOptions<TenantTheme>
        {
            Query = item => item.ThemeId == themeId,
            CancellationToken = cancellationToken,
        });

        return [.. rows.Select(ToRecord)];
    }

    public async Task SaveTenantThemeAsync(TenantThemeRecord record, string? updatedBy, CancellationToken cancellationToken = default)
    {
        var context = ContextOptions.ForCancellationToken(cancellationToken);
        string? configuration = record.Configuration.Count == 0 ? null : JsonSerializer.Serialize(record.Configuration);

        var row = await tenantThemes.FindOneAsync(new SearchOptions<TenantTheme>
        {
            Query = item => item.TenantId == record.TenantId && item.ThemeId == record.ThemeId,
            CancellationToken = cancellationToken,
        });

        if (row is null)
        {
            await tenantThemes.InsertAsync(new TenantTheme
            {
                TenantId = record.TenantId,
                ThemeId = record.ThemeId,
                IsGranted = record.IsGranted,
                IsEnabled = record.IsEnabled,
                Configuration = configuration,
                UpdatedAtUtc = Now(),
                UpdatedBy = updatedBy,
            }, context);
            return;
        }

        row.IsGranted = record.IsGranted;
        row.IsEnabled = record.IsEnabled;
        row.Configuration = configuration;
        row.UpdatedAtUtc = Now();
        row.UpdatedBy = updatedBy;
        await tenantThemes.UpdateAsync(row, context);
    }

    public Task<string?> GetUserThemeAsync(string tenantId, string userId, CancellationToken cancellationToken = default) =>
        userPreferences.FindOneAsync(
            new SearchOptions<UserThemePreference>
            {
                Query = item => item.TenantId == tenantId && item.UserId == userId,
                CancellationToken = cancellationToken,
            },
            item => (string?)item.ThemeId);

    public async Task SetUserThemeAsync(string tenantId, string userId, string? themeId, CancellationToken cancellationToken = default)
    {
        var context = ContextOptions.ForCancellationToken(cancellationToken);
        if (themeId is null)
        {
            await userPreferences.DeleteAsync(item => item.TenantId == tenantId && item.UserId == userId, context);
            return;
        }

        var now = Now();
        int updated = await userPreferences.UpdateAsync(
            item => item.TenantId == tenantId && item.UserId == userId,
            setters => setters
                .SetProperty(item => item.ThemeId, themeId)
                .SetProperty(item => item.UpdatedAtUtc, now),
            context);

        if (updated == 0)
        {
            await userPreferences.InsertAsync(new UserThemePreference
            {
                TenantId = tenantId,
                UserId = userId,
                ThemeId = themeId,
                UpdatedAtUtc = now,
            }, context);
        }
    }

    private TenantThemeRecord ToRecord(TenantTheme row) =>
        new(row.TenantId, row.ThemeId, row.IsGranted, row.IsEnabled, ReadConfiguration(row));

    /// <summary>A damaged value is treated as no saved settings, so the theme falls back to its defaults.</summary>
    private IReadOnlyDictionary<string, string> ReadConfiguration(TenantTheme row)
    {
        if (string.IsNullOrWhiteSpace(row.Configuration))
        {
            return ReadOnlyDictionary<string, string>.Empty;
        }

        try
        {
            var values = JsonSerializer.Deserialize<Dictionary<string, string>>(row.Configuration);
            return values is null
                ? ReadOnlyDictionary<string, string>.Empty
                : new ReadOnlyDictionary<string, string>(new Dictionary<string, string>(values, StringComparer.Ordinal));
        }
        catch (JsonException exception)
        {
            logger.LogWarning(exception, "The settings of theme {ThemeId} for tenant {TenantId} could not be read; its defaults are used", row.ThemeId, row.TenantId);
            return ReadOnlyDictionary<string, string>.Empty;
        }
    }

    private DateTime Now() => clock.GetUtcNow().UtcDateTime;
}
