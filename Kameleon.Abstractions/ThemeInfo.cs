namespace Kameleon;

/// <summary>
/// A theme that passed validation and is in the catalog. Asset paths are already resolved against
/// <see cref="AssetBasePath"/>, so a component never builds a theme URL by hand.
/// </summary>
public sealed record ThemeInfo
{
    /// <summary>
    /// Gets or sets the identifier.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Gets or sets the name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets or sets the version.
    /// </summary>
    public required string Version { get; init; }

    /// <summary>
    /// Gets or sets the description.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Gets or sets the author.
    /// </summary>
    public string? Author { get; init; }

    /// <summary>
    /// Gets or sets the kind.
    /// </summary>
    public required ThemeKind Kind { get; init; }

    /// <summary>
    /// Where the theme's assets are served from, relative to the document's base address and ending with
    /// <c>/</c>: <c>_content/{Assembly}/</c> for a compiled theme, <c>themes/{id}/{version}/</c> for a static one.
    /// Relative so that a tenant path prefix in <c>&lt;base href&gt;</c> is kept.
    /// </summary>
    public required string AssetBasePath { get; init; }

    /// <summary>Empty for a static theme, which renders inside the host's layout.</summary>
    public IReadOnlyList<ThemeLayoutInfo> Layouts { get; init; } = [];

    /// <summary>Null for a static theme.</summary>
    public ThemeLayoutInfo? DefaultLayout { get; init; }

    /// <summary>
    /// Gets or sets the stylesheet urls.
    /// </summary>
    public IReadOnlyList<string> StylesheetUrls { get; init; } = [];

    /// <summary>
    /// Gets or sets the script urls.
    /// </summary>
    public IReadOnlyList<string> ScriptUrls { get; init; } = [];

    /// <summary>
    /// Gets or sets the preview image URL.
    /// </summary>
    public string? PreviewImageUrl { get; init; }

    /// <summary>
    /// Gets or sets the views.
    /// </summary>
    public IReadOnlyDictionary<string, Type> Views { get; init; } = ReadOnlyDictionary<string, Type>.Empty;

    /// <summary>
    /// Gets or sets the settings.
    /// </summary>
    public IReadOnlyList<ThemeSettingDefinition> Settings { get; init; } = [];

    /// <summary>
    /// Gets or sets a value indicating whether this instance is supports right to left.
    /// </summary>
    public bool SupportsRightToLeft { get; init; }

    /// <summary>The assembly or folder the theme came from, for diagnostics.</summary>
    public required string Source { get; init; }

    /// <summary>
    /// The URL of one of the theme's assets, such as <c>images/logo.svg</c>. An absolute <c>http</c> or
    /// <c>https</c> address is returned unchanged.
    /// </summary>
    public string Asset(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return AssetBasePath;
        }

        if (Uri.TryCreate(path, UriKind.Absolute, out var uri) && (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp))
        {
            return path;
        }

        return AssetBasePath + path.TrimStart('/');
    }

    /// <summary>The layout with this id, or null when the theme has none by that id.</summary>
    public ThemeLayoutInfo? FindLayout(string? layoutId) =>
        string.IsNullOrEmpty(layoutId)
            ? null
            : Layouts.FirstOrDefault(layout => string.Equals(layout.Id, layoutId, StringComparison.Ordinal));
}

/// <summary>
/// Represents the theme layout info.
/// </summary>
/// <param name="Id">The identifier.</param>
/// <param name="Name">The name.</param>
/// <param name="Description">The description.</param>
/// <param name="ComponentType">The component type.</param>
public sealed record ThemeLayoutInfo(string Id, string Name, string? Description, Type ComponentType);
