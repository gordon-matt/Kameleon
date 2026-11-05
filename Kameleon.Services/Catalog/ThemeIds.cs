namespace Kameleon.Catalog;

/// <summary>The one format for theme, layout and setting identifiers, so every store and URL can hold them as-is.</summary>
internal static partial class ThemeIds
{
    public const int MaxLength = 64;

    public static bool IsValid(string? id) => id is { Length: > 0 and <= MaxLength } && Pattern().IsMatch(id);

    [GeneratedRegex("^[a-z0-9]+(?:-[a-z0-9]+)*$", RegexOptions.CultureInvariant)]
    private static partial Regex Pattern();
}
