namespace Kameleon;

public enum ThemeKind
{
    /// <summary>An <see cref="ITheme"/> in an assembly: its own layouts, views and assets.</summary>
    Compiled,

    /// <summary>A folder with a <c>theme.json</c>, stylesheets and images. It uses the host's layout.</summary>
    Static,
}
