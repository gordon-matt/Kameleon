namespace Kameleon;

/// <summary>
/// A compiled theme. Register it with <see cref="ThemeServiceCollectionExtensions.AddKameleonTheme{TTheme}"/>,
/// either from the host or from a plugin while the host builds its services. One instance is read when the
/// catalog is built, so it must not hold per-request or per-tenant state.
/// </summary>
public interface ITheme
{
    /// <summary>
    /// Gets the descriptor.
    /// </summary>
    ThemeDescriptor Descriptor { get; }

    /// <summary>
    /// At least one. The first is the theme's default layout unless <see cref="ThemeDescriptor.DefaultLayoutId"/>
    /// names another.
    /// </summary>
    IReadOnlyList<ThemeLayoutDescriptor> Layouts { get; }

    /// <summary>
    /// Replacements for components the host renders through <see cref="ThemeView"/>, keyed by the view name
    /// the host documents. A theme cannot replace anything the host does not render that way.
    /// </summary>
    IReadOnlyDictionary<string, Type> Views => ReadOnlyDictionary<string, Type>.Empty;

    /// <summary>Values an administrator can change for each tenant, such as an accent colour or a tagline.</summary>
    IReadOnlyList<ThemeSettingDefinition> Settings => [];
}
