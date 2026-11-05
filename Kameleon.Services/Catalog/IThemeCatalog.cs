namespace Kameleon.Catalog;

/// <summary>
/// Every theme that is installed and valid, whoever may use it. Eligibility for a tenant is decided elsewhere;
/// being in the catalog grants nothing.
/// </summary>
public interface IThemeCatalog
{
    /// <summary>Ordered by name.</summary>
    IReadOnlyList<ThemeInfo> Themes { get; }

    /// <summary>Themes that were found but not registered, and why.</summary>
    IReadOnlyList<ThemeCatalogError> Errors { get; }

    ThemeInfo? Find(string? themeId);

    /// <summary>The folder a static theme's assets are served from, or null for any other theme.</summary>
    string? GetContentRoot(string themeId);

    /// <summary>
    /// Reads every source again, for static themes added or changed on disk. Compiled themes only change when
    /// the application restarts. Callers should also clear cached resolutions.
    /// </summary>
    void Reload();
}
