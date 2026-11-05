namespace Kameleon.FileSystem;

/// <summary>The <c>theme.json</c> of a static theme. Unknown properties are ignored so later schema versions can add some.</summary>
internal sealed record ThemeManifest
{
    public const int CurrentSchemaVersion = 1;

    public int SchemaVersion { get; init; }

    /// <summary>When omitted, the folder name in lowercase.</summary>
    public string? Id { get; init; }

    public string? Name { get; init; }

    public string? Version { get; init; }

    public string? Description { get; init; }

    public string? Author { get; init; }

    public string? Preview { get; init; }

    public List<string>? Stylesheets { get; init; }

    public List<string>? Scripts { get; init; }

    public bool SupportsRightToLeft { get; init; }

    public List<ThemeManifestSetting>? Settings { get; init; }
}

internal sealed record ThemeManifestSetting
{
    public string? Key { get; init; }

    public string? Name { get; init; }

    public ThemeSettingKind Kind { get; init; } = ThemeSettingKind.Text;

    public string? Default { get; init; }

    public string? Description { get; init; }

    public List<string>? Choices { get; init; }

    public string? CssVariable { get; init; }

    public int? MaxLength { get; init; }

    public ThemeSettingDefinition ToDefinition() => new()
    {
        Key = Key ?? string.Empty,
        Name = Name ?? Key ?? string.Empty,
        Kind = Kind,
        DefaultValue = Default,
        Description = Description,
        Choices = Choices ?? [],
        CssVariable = CssVariable,
        MaxLength = MaxLength ?? 256,
    };
}
