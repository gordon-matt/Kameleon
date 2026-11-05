namespace Kameleon;

/// <summary>Who a theme is and which assets it brings. Every value is validated before the theme is registered.</summary>
public sealed record ThemeDescriptor
{
    /// <summary>
    /// The key stored in settings: lowercase letters, digits and single hyphens, at most 64 characters.
    /// Changing it after release orphans every setting that names the theme.
    /// </summary>
    public required string Id { get; init; }

    public required string Name { get; init; }

    /// <summary>
    /// A semantic version such as <c>1.2.0</c> or <c>2.0.0-beta.1</c>. When two themes share an
    /// <see cref="Id"/>, the higher version is registered and the other is reported.
    /// </summary>
    public string Version { get; init; } = "1.0.0";

    public string? Description { get; init; }

    public string? Author { get; init; }

    /// <summary>An asset path, relative to the theme's assets, of an image shown on the admin screens.</summary>
    public string? PreviewImage { get; init; }

    /// <summary>Asset paths, relative to the theme's assets, that <c>ThemeHead</c> links in this order.</summary>
    public IReadOnlyList<string> Stylesheets { get; init; } = [];

    /// <summary>Asset paths of scripts that <c>ThemeHead</c> loads with <c>defer</c>.</summary>
    public IReadOnlyList<string> Scripts { get; init; } = [];

    /// <summary>The layout used when nobody has chosen one. Null means the first in <see cref="ITheme.Layouts"/>.</summary>
    public string? DefaultLayoutId { get; init; }

    public bool SupportsRightToLeft { get; init; }
}
