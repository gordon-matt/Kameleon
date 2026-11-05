namespace Kameleon.Catalog;

/// <summary>
/// A semantic version, compared so that a release outranks its own pre-releases. Also restricts the characters
/// allowed, because a static theme's version is part of its asset URLs.
/// </summary>
internal readonly partial record struct ThemeVersion(Version Number, string? Prerelease) : IComparable<ThemeVersion>
{
    public const int MaxLength = 32;

    public static bool TryParse(string? value, out ThemeVersion version)
    {
        version = default;
        if (value is null || value.Length > MaxLength || !Pattern().IsMatch(value))
        {
            return false;
        }

        int dash = value.IndexOf('-');
        string number = dash < 0 ? value : value[..dash];
        if (!number.Contains('.'))
        {
            number += ".0";
        }

        if (!Version.TryParse(number, out var parsed))
        {
            return false;
        }

        version = new ThemeVersion(parsed, dash < 0 ? null : value[(dash + 1)..]);
        return true;
    }

    public int CompareTo(ThemeVersion other)
    {
        int byNumber = Number.CompareTo(other.Number);
        if (byNumber != 0)
        {
            return byNumber;
        }

        if (Prerelease is null)
        {
            return other.Prerelease is null ? 0 : 1;
        }

        return other.Prerelease is null ? -1 : string.CompareOrdinal(Prerelease, other.Prerelease);
    }

    [GeneratedRegex(@"^\d+(\.\d+){0,3}(-[0-9A-Za-z.]+)?$", RegexOptions.CultureInvariant)]
    private static partial Regex Pattern();
}
