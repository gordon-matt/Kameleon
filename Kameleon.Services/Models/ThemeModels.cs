namespace Kameleon.Models;

/// <summary>
/// Represents the theme summary DTO.
/// </summary>
/// <param name="Id">The identifier.</param>
/// <param name="Name">The name.</param>
/// <param name="Version">The version.</param>
/// <param name="Description">The description.</param>
/// <param name="Author">The author.</param>
/// <param name="Kind">The kind.</param>
/// <param name="PreviewImageUrl">The preview image URL.</param>
/// <param name="Layouts">The layouts.</param>
/// <param name="DefaultLayoutId">The default layout identifier.</param>
/// <param name="SupportsRightToLeft">The supports right to left.</param>
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
    /// <summary>
    /// Creates a value from the value.
    /// </summary>
    /// <param name="theme">The theme.</param>
    /// <returns>The theme summary.</returns>
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

/// <summary>
/// Represents the theme layout DTO.
/// </summary>
/// <param name="Id">The identifier.</param>
/// <param name="Name">The name.</param>
/// <param name="Description">The description.</param>
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

/// <summary>
/// Represents the catalog theme DTO.
/// </summary>
/// <param name="Theme">The theme.</param>
/// <param name="IsEnabled">The is enabled.</param>
/// <param name="Availability">The availability.</param>
/// <param name="IsFallback">The is fallback.</param>
/// <param name="Source">The source.</param>
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

/// <summary>
/// Represents the theme tenant grant DTO.
/// </summary>
/// <param name="TenantId">The tenant identifier.</param>
/// <param name="TenantName">The tenant name.</param>
/// <param name="IsGranted">The is granted.</param>
public sealed record ThemeTenantGrantDto(string TenantId, string TenantName, bool IsGranted);

/// <param name="CanChoose">The tenant allows a choice, the user is signed in, and there is more than one theme to pick.</param>
/// <param name="SelectedThemeId">The user's saved choice, or null when they follow the default.</param>
public sealed record UserThemeChoicesDto(
    bool CanChoose,
    string? SelectedThemeId,
    string? DefaultThemeId,
    IReadOnlyList<ThemeSummaryDto> Themes);

/// <summary>
/// Represents the set default theme request.
/// </summary>
public sealed record SetDefaultThemeRequest
{
    /// <summary>
    /// Gets or sets the theme identifier.
    /// </summary>
    public string ThemeId { get; init; } = string.Empty;

    /// <summary>Null uses the theme's own default layout.</summary>
    public string? LayoutId { get; init; }
}

/// <summary>
/// Represents the save theme settings request.
/// </summary>
public sealed record SaveThemeSettingsRequest
{
    /// <summary>
    /// Gets or sets the theme identifier.
    /// </summary>
    public string ThemeId { get; init; } = string.Empty;

    /// <summary>An empty or null value returns that setting to the theme's default.</summary>
    public IReadOnlyDictionary<string, string?> Values { get; init; } = new Dictionary<string, string?>();
}
