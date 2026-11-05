namespace Kameleon.Resolution;

/// <summary>Decides which theme and layout apply to the current tenant and user.</summary>
public interface IThemeResolver
{
    /// <summary>
    /// In order: the user's choice, when the tenant allows one and the theme is eligible there; the tenant's
    /// default, when it is eligible; the application's fallback theme; no theme. Never throws for missing or
    /// invalid settings, so a broken theme cannot take the site down.
    /// </summary>
    Task<ThemeContext> ResolveAsync(CancellationToken cancellationToken = default);
}
