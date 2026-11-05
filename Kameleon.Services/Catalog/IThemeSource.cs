using System.Collections.ObjectModel;

namespace Kameleon.Catalog;

/// <summary>
/// Somewhere themes come from. Kameleon ships one for compiled themes and one for static theme folders; a host
/// can add its own. Everything a source returns is validated by the catalog, so a source does not need to.
/// </summary>
public interface IThemeSource
{
    /// <summary>Called when the catalog is built and each time it is reloaded.</summary>
    ThemeSourceResult Load();
}

/// <param name="Errors">Themes the source found but could not read, such as a malformed <c>theme.json</c>.</param>
public sealed record ThemeSourceResult(IReadOnlyList<ThemeCandidate> Themes, IReadOnlyList<ThemeCatalogError> Errors)
{
    public static ThemeSourceResult Empty { get; } = new([], []);
}

/// <summary>A theme as a source found it, before validation.</summary>
public sealed record ThemeCandidate
{
    /// <summary>The assembly or folder, shown with any error about the theme.</summary>
    public required string Source { get; init; }

    public required ThemeKind Kind { get; init; }

    public required ThemeDescriptor Descriptor { get; init; }

    public IReadOnlyList<ThemeLayoutDescriptor> Layouts { get; init; } = [];

    public IReadOnlyDictionary<string, Type> Views { get; init; } = ReadOnlyDictionary<string, Type>.Empty;

    public IReadOnlyList<ThemeSettingDefinition> Settings { get; init; } = [];

    /// <inheritdoc cref="ThemeInfo.AssetBasePath"/>
    public required string AssetBasePath { get; init; }

    /// <summary>For a static theme, the folder its assets are served from. Null for a compiled theme.</summary>
    public string? ContentRoot { get; init; }
}

/// <param name="ThemeId">Null when the source could not tell which theme it was.</param>
public sealed record ThemeCatalogError(string Source, string? ThemeId, string Message);
