using Heroicsoft.NetForge.Components.Navigation;

namespace Kameleon;

/// <summary>Puts the theme screens on the host menu. The host places it with <c>AddKameleon()</c>.</summary>
public sealed class KameleonMenuContributor(IOptions<KameleonOptions> options) : INetForgeMenuContributor
{
    /// <summary>
    /// Gets the name.
    /// </summary>
    public string Name => "Kameleon";

    /// <summary>
    /// Gets the default menu.
    /// </summary>
    public string DefaultMenu => "Appearance";

    /// <summary>
    /// Performs the contribute operation.
    /// </summary>
    /// <param name="group">The group.</param>
    /// <param name="configure">The configure.</param>
    public void Contribute(INetForgeMenuGroupBuilder group, Action<NetForgeMenuItemOptions>? configure) => group.AddCustom(item =>
    {
        var settings = options.Value;
        item.Title = "Themes";
        item.Icon = "palette";
        item.Url = settings.AdminPath;
        item.Roles = Roles(settings.ManagerRoles);
        item.AddChild(child =>
        {
            child.Title = "Site themes";
            child.Icon = "palette";
            child.Url = settings.AdminPath;
            child.Roles = Roles(settings.ManagerRoles);
        });
        item.AddChild(child =>
        {
            child.Title = "Theme catalog";
            child.Icon = "grid";
            child.Url = settings.AdminPath + "/catalog";
            child.Roles = Roles(settings.CatalogManagerRoles);
        });
        configure?.Invoke(item);
    });

    /// <remarks>Only hides the links: the pages and services check the policies themselves.</remarks>
    private static string? Roles(IList<string> roles) => roles.Count == 0 ? null : string.Join(',', roles);
}

/// <summary>Places the Kameleon contributor. The link keeps the library defaults unless <paramref name="configure"/> replaces them.</summary>
public static class KameleonMenuExtensions
{
    extension(INetForgeMenuGroupBuilder menu)
    {
        /// <summary>
        /// Adds the kameleon.
        /// </summary>
        /// <param name="configure">The configure.</param>
        /// <returns>The kameleon.</returns>
        public INetForgeMenuGroupBuilder AddKameleon(Action<NetForgeMenuItemOptions>? configure = null) =>
            menu.AddContributor("Kameleon", configure);
    }
}
