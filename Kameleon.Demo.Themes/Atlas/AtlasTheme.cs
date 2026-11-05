namespace Kameleon.Demo.Themes.Atlas;

/// <summary>A dashboard-style theme: navigation down the left, dense content, its own article cards.</summary>
public sealed class AtlasTheme : ITheme
{
    public ThemeDescriptor Descriptor { get; } = new()
    {
        Id = "atlas",
        Name = "Atlas",
        Version = "1.0.0",
        Description = "A compact theme with navigation down the left and its own article cards.",
        Author = "Kameleon samples",
        PreviewImage = "atlas/preview.svg",
        Stylesheets = ["atlas/atlas.css"],
    };

    public IReadOnlyList<ThemeLayoutDescriptor> Layouts { get; } =
    [
        new("default", "Side navigation", typeof(AtlasLayout)),
    ];

    public IReadOnlyDictionary<string, Type> Views { get; } = new Dictionary<string, Type>
    {
        [DemoViews.Article] = typeof(AtlasArticle),
    };

    public IReadOnlyList<ThemeSettingDefinition> Settings { get; } =
    [
        new()
        {
            Key = "accent",
            Name = "Accent colour",
            Kind = ThemeSettingKind.Color,
            DefaultValue = "#c2410c",
            CssVariable = "--atlas-accent",
        },
        new()
        {
            Key = "density",
            Name = "Density",
            Kind = ThemeSettingKind.Choice,
            Choices = ["comfortable", "compact"],
            DefaultValue = "comfortable",
        },
    ];
}
