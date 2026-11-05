using System.Collections.ObjectModel;
using System.Reflection;

namespace Kameleon.Catalog;

/// <summary>The themes registered with <c>AddKameleonTheme</c>, by the host or by a plugin it loaded.</summary>
internal sealed class CompiledThemeSource(IEnumerable<ITheme> themes) : IThemeSource
{
    public ThemeSourceResult Load()
    {
        var candidates = new List<ThemeCandidate>();
        var errors = new List<ThemeCatalogError>();

        foreach (var theme in themes)
        {
            var type = theme.GetType();
            var assemblyName = type.Assembly.GetName();
            string source = $"{type.FullName} ({assemblyName.Name} {assemblyName.Version})";

            try
            {
                candidates.Add(new ThemeCandidate
                {
                    Source = source,
                    Kind = ThemeKind.Compiled,
                    Descriptor = theme.Descriptor,
                    Layouts = theme.Layouts ?? [],
                    Views = theme.Views ?? ReadOnlyDictionary<string, Type>.Empty,
                    Settings = theme.Settings ?? [],
                    AssetBasePath = AssetBasePathFor(type.Assembly),
                });
            }
            catch (Exception exception)
            {
                errors.Add(new ThemeCatalogError(source, null, $"The theme could not be read: {exception.Message}"));
            }
        }

        return new ThemeSourceResult(candidates, errors);
    }

    /// <summary>
    /// A Razor class library's <c>wwwroot</c> is served under <c>_content/{AssemblyName}</c>, while the
    /// application's own is served from the root.
    /// </summary>
    internal static string AssetBasePathFor(Assembly assembly) =>
        assembly == Assembly.GetEntryAssembly() ? string.Empty : $"_content/{assembly.GetName().Name}/";
}
