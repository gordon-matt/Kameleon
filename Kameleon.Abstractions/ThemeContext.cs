namespace Kameleon;

/// <summary>
/// The theme in effect for the current request or circuit. It is resolved on the server from the tenant's
/// settings, never from anything the browser sends, and is passed to theme components as a cascading value.
/// </summary>
public sealed record ThemeContext
{
    /// <summary>Empty for a single-tenant site.</summary>
    public required string TenantId { get; init; }

    /// <summary>Null for an anonymous visitor.</summary>
    public string? UserId { get; init; }

    /// <summary>Null when no theme could be resolved, in which case the host's own layout is used.</summary>
    public ThemeInfo? Theme { get; init; }

    /// <summary>Null for a static theme or when <see cref="Theme"/> is null.</summary>
    public ThemeLayoutInfo? Layout { get; init; }

    /// <summary>True when the visitor's own choice replaced the tenant's default theme.</summary>
    public bool IsUserOverride { get; init; }

    /// <summary>The theme's settings: its defaults with the tenant's saved values on top.</summary>
    public IReadOnlyDictionary<string, string> Settings { get; init; } = ReadOnlyDictionary<string, string>.Empty;

    public string? ThemeId => Theme?.Id;

    public static ThemeContext None(string tenantId, string? userId) => new() { TenantId = tenantId, UserId = userId };

    /// <inheritdoc cref="ThemeInfo.Asset"/>
    public string Asset(string path) => Theme?.Asset(path) ?? path;

    public string? Setting(string key) => Settings.GetValueOrDefault(key);

    /// <summary>True when a <see cref="ThemeSettingKind.Boolean"/> setting is on.</summary>
    public bool IsSettingOn(string key) => string.Equals(Setting(key), "true", StringComparison.OrdinalIgnoreCase);
}
