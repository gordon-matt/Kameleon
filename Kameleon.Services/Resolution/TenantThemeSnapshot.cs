namespace Kameleon.Resolution;

/// <summary>Everything stored about one tenant's themes, read together so a resolution is consistent.</summary>
internal sealed record TenantThemeSnapshot(
    string TenantId,
    TenantThemeSettingsRecord? Settings,
    IReadOnlyDictionary<string, TenantThemeRecord> Themes,
    IReadOnlyDictionary<string, ThemeCatalogRecord> Catalog)
{
    public static async Task<TenantThemeSnapshot> LoadAsync(IThemeSettingsStore store, string tenantId, CancellationToken cancellationToken)
    {
        var catalog = await store.GetCatalogAsync(cancellationToken);
        var settings = await store.GetTenantSettingsAsync(tenantId, cancellationToken);
        var themes = await store.GetTenantThemesAsync(tenantId, cancellationToken);

        return new TenantThemeSnapshot(
            tenantId,
            settings,
            themes.ToDictionary(theme => theme.ThemeId, StringComparer.Ordinal),
            catalog.ToDictionary(entry => entry.ThemeId, StringComparer.Ordinal));
    }
}
