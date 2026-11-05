namespace Kameleon.Models;

public sealed record ThemeSummaryDto(
    string Id,
    string Name,
    string Version,
    string? Description,
    string? Author,
    ThemeKind Kind,
    string? PreviewImageUrl,
    IReadOnlyList<ThemeLayoutDto> Layouts,
    string? DefaultLayoutId,
    bool SupportsRightToLeft)
{
    public static ThemeSummaryDto From(ThemeInfo theme) => new(
        theme.Id,
        theme.Name,
        theme.Version,
        theme.Description,
        theme.Author,
        theme.Kind,
        theme.PreviewImageUrl,
        [.. theme.Layouts.Select(layout => new ThemeLayoutDto(layout.Id, layout.Name, layout.Description))],
        theme.DefaultLayout?.Id,
        theme.SupportsRightToLeft);
}

public sealed record ThemeLayoutDto(string Id, string Name, string? Description);

/// <summary>A theme the current tenant may use, with the tenant's choices about it.</summary>
public sealed record TenantThemeDto(
    ThemeSummaryDto Theme,
    bool IsEnabled,
    bool IsDefault,
    IReadOnlyList<ThemeSettingDefinition> SettingDefinitions,
    IReadOnlyDictionary<string, string> Settings);

/// <param name="DefaultIsUnavailable">
/// The saved default is no longer installed or available, so visitors see the fallback theme. The saved value
/// is kept in case the theme comes back; choosing another default clears the warning.
/// </param>
public sealed record TenantThemesDto(
    string? DefaultThemeId,
    string? DefaultLayoutId,
    bool AllowUserOverride,
    bool DefaultIsUnavailable,
    string? FallbackThemeId,
    IReadOnlyList<TenantThemeDto> Themes);

public sealed record CatalogThemeDto(
    ThemeSummaryDto Theme,
    bool IsEnabled,
    ThemeAvailability Availability,
    bool IsFallback,
    string Source);

/// <param name="CanGrantPerTenant">False when the host has not registered an <c>IThemeTenantDirectory</c>.</param>
public sealed record ThemeCatalogDto(
    IReadOnlyList<CatalogThemeDto> Themes,
    IReadOnlyList<ThemeCatalogError> Errors,
    bool CanGrantPerTenant);

public sealed record ThemeTenantGrantDto(string TenantId, string TenantName, bool IsGranted);

/// <param name="CanChoose">The tenant allows a choice, the user is signed in, and there is more than one theme to pick.</param>
/// <param name="SelectedThemeId">The user's saved choice, or null when they follow the default.</param>
public sealed record UserThemeChoicesDto(
    bool CanChoose,
    string? SelectedThemeId,
    string? DefaultThemeId,
    IReadOnlyList<ThemeSummaryDto> Themes);

public sealed record SetDefaultThemeRequest
{
    public string ThemeId { get; init; } = string.Empty;

    /// <summary>Null uses the theme's own default layout.</summary>
    public string? LayoutId { get; init; }
}

public sealed record SaveThemeSettingsRequest
{
    public string ThemeId { get; init; } = string.Empty;

    /// <summary>An empty or null value returns that setting to the theme's default.</summary>
    public IReadOnlyDictionary<string, string?> Values { get; init; } = new Dictionary<string, string?>();
}
