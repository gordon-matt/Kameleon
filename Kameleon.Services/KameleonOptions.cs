namespace Kameleon;

/// <summary>
/// Bind from the <c>Kameleon</c> configuration section or set in code through <c>AddKameleon</c>. The
/// defaults here apply to any tenant that has not saved settings of its own.
/// </summary>
public sealed class KameleonOptions
{
    /// <summary>
    /// The section name constant.
    /// </summary>
    public const string SectionName = "Kameleon";

    /// <summary>
    /// The default asset request path constant.
    /// </summary>
    public const string DefaultAssetRequestPath = "/themes";

    /// <summary>
    /// The default admin path constant.
    /// </summary>
    public const string DefaultAdminPath = "/admin/themes";

    /// <summary>
    /// The theme used when a tenant's default is missing, disabled or not available to it. It is the site's
    /// safety net, so it is used even when no tenant has enabled it, and it cannot be disabled globally.
    /// When null and nothing else resolves, pages use <see cref="HostLayout"/> with no theme.
    /// </summary>
    public string? FallbackThemeId { get; set; }

    /// <summary>The default theme of a tenant that has not chosen one.</summary>
    public string? DefaultThemeId { get; set; }

    /// <summary>A layout of <see cref="DefaultThemeId"/>. Null means that theme's own default layout.</summary>
    public string? DefaultLayoutId { get; set; }

    /// <summary>Whether users may choose their own theme in a tenant that has not decided.</summary>
    public bool AllowUserOverride { get; set; }

    /// <summary>
    /// Themes a tenant offers until its administrator turns them on or off. A theme registered in the catalog
    /// is not offered to anyone unless it is listed here or a tenant enables it.
    /// </summary>
    public IList<string> EnabledThemes { get; set; } = [];

    /// <summary>
    /// Availability of a theme that no administrator has set. <see cref="ThemeAvailability.AllTenants"/> suits
    /// a single site; a host that sells themes per tenant sets <see cref="ThemeAvailability.SelectedTenants"/>.
    /// </summary>
    public ThemeAvailability DefaultAvailability { get; set; } = ThemeAvailability.AllTenants;

    /// <summary>
    /// The folder of static themes, relative to the content root. Each subfolder with a <c>theme.json</c> is a
    /// theme. Null turns static themes off.
    /// </summary>
    public string? ThemesPath { get; set; } = "Themes";

    /// <summary>The URL prefix static theme assets are served under by <c>MapKameleon</c>.</summary>
    public string AssetRequestPath { get; set; } = DefaultAssetRequestPath;

    /// <summary>
    /// Static themes are files anyone with access to the folder can change, so their <c>scripts</c> are rejected
    /// unless this is on. Compiled themes are trusted code and are not affected.
    /// </summary>
    public bool AllowStaticThemeScripts { get; set; }

    /// <summary>
    /// How long a tenant's settings and a user's choice are cached. Changes made through Kameleon's services
    /// clear the cache at once on this server; other servers in a farm see them when this expires.
    /// Zero turns caching off.
    /// </summary>
    public TimeSpan CacheDuration { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>Applies Kameleon's migrations at startup. Turn off when schema changes go through a pipeline.</summary>
    public bool AutoMigrate { get; set; } = true;

    /// <summary>
    /// Roles that may change the current tenant's theme settings. Empty lets any signed-in user in. Ignored when
    /// the host registers its own <c>Kameleon.Manage</c> policy.
    /// </summary>
    public IList<string> ManagerRoles { get; set; } = ["Administrator"];

    /// <summary>
    /// Roles that may change application-wide theme settings and decide which tenants get which themes. Ignored
    /// when the host registers its own <c>Kameleon.ManageCatalog</c> policy.
    /// </summary>
    public IList<string> CatalogManagerRoles { get; set; } = ["Administrator"];

    /// <summary>
    /// Where the theme screens live: the site's settings here and the catalog under <c>/catalog</c>. The
    /// pages Kameleon ships answer only at <see cref="DefaultAdminPath"/>, because Blazor routes are fixed
    /// when the library is compiled; any other value needs host pages that render the screen components.
    /// </summary>
    public string AdminPath { get; set; } = DefaultAdminPath;

    /// <summary>Layout of the theme screens. When null the NetForge shell layout is used.</summary>
    public Type? AdminLayout { get; set; }

    /// <summary>
    /// The host's own layout. Static themes render inside it, and so does every page when no theme resolves.
    /// When null, a plain layout with the conventional regions is used.
    /// </summary>
    public Type? HostLayout { get; set; }

    /// <summary>
    /// Components the host shows in a region when a page supplies nothing for it, such as its menu in
    /// <see cref="ThemeRegions.PrimaryNavigation"/>. Fill it with <see cref="AddRegionContent{TComponent}"/>.
    /// </summary>
    public IDictionary<string, IList<Type>> RegionContent { get; } = new Dictionary<string, IList<Type>>(StringComparer.Ordinal);

    /// <summary>
    /// Adds the region content.
    /// </summary>
    /// <typeparam name="TComponent">The type of the component.</typeparam>
    /// <param name="region">The region.</param>
    /// <returns>The region content.</returns>
    public KameleonOptions AddRegionContent<TComponent>(string region)
        where TComponent : Microsoft.AspNetCore.Components.IComponent
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(region);

        if (!RegionContent.TryGetValue(region, out var components))
        {
            components = [];
            RegionContent[region] = components;
        }

        if (!components.Contains(typeof(TComponent)))
        {
            components.Add(typeof(TComponent));
        }

        return this;
    }
}
