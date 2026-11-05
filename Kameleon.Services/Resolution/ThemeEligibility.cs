namespace Kameleon.Resolution;

/// <summary>
/// Which themes a tenant may use. A theme is eligible when it is installed, not disabled globally, available to
/// the tenant, and either enabled by the tenant or its default. Every theme lookup goes through here, so a theme
/// granted to one tenant is never visible to another.
/// </summary>
internal static class ThemeEligibility
{
    /// <summary>Installed, not disabled globally, and available to this tenant. The tenant may enable it.</summary>
    public static bool IsGranted(ThemeInfo theme, TenantThemeSnapshot snapshot, KameleonOptions options)
    {
        var entry = snapshot.Catalog.GetValueOrDefault(theme.Id);
        if (entry is { IsEnabled: false })
        {
            return false;
        }

        var availability = entry?.Availability ?? options.DefaultAvailability;
        return availability == ThemeAvailability.AllTenants
            || snapshot.Themes.GetValueOrDefault(theme.Id)?.IsGranted == true;
    }

    /// <summary>The tenant's own choice when it has made one, otherwise the application default.</summary>
    public static bool IsEnabledByTenant(string themeId, TenantThemeSnapshot snapshot, KameleonOptions options) =>
        snapshot.Themes.TryGetValue(themeId, out var record)
            ? record.IsEnabled
            : options.EnabledThemes.Contains(themeId, StringComparer.Ordinal);

    public static bool IsEligible(ThemeInfo theme, TenantThemeSnapshot snapshot, KameleonOptions options) =>
        IsGranted(theme, snapshot, options)
        && (IsEnabledByTenant(theme.Id, snapshot, options)
            || string.Equals(theme.Id, Default(snapshot, options).ThemeId, StringComparison.Ordinal));

    public static IReadOnlyList<ThemeInfo> Eligible(IThemeCatalog catalog, TenantThemeSnapshot snapshot, KameleonOptions options) =>
        [.. catalog.Themes.Where(theme => IsEligible(theme, snapshot, options))];

    /// <summary>
    /// The tenant's saved default with its layout, or the application's when the tenant has not chosen a theme.
    /// The layout always travels with its theme, so a layout id is never applied to another theme.
    /// </summary>
    public static (string? ThemeId, string? LayoutId) Default(TenantThemeSnapshot snapshot, KameleonOptions options) =>
        snapshot.Settings?.DefaultThemeId is { } themeId
            ? (themeId, snapshot.Settings.DefaultLayoutId)
            : (options.DefaultThemeId, options.DefaultLayoutId);

    public static bool AllowUserOverride(TenantThemeSnapshot snapshot, KameleonOptions options) =>
        snapshot.Settings?.AllowUserOverride ?? options.AllowUserOverride;
}
