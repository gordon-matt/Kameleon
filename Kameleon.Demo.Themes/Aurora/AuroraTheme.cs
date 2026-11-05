namespace Kameleon.Demo.Themes.Aurora;

/// <summary>A light editorial theme: a header across the top, a sidebar on the right.</summary>
public sealed class AuroraTheme : ITheme
{
    public ThemeDescriptor Descriptor { get; } = new()
    {
        Id = "aurora",
        Name = "Aurora",
        Version = "1.0.0",
        Description = "An airy editorial theme with a top header and a sidebar on the right.",
        Author = "Kameleon samples",
        PreviewImage = "aurora/preview.svg",
        Stylesheets = ["aurora/aurora.css"],
        DefaultLayoutId = "default",
    };

    public IReadOnlyList<ThemeLayoutDescriptor> Layouts { get; } =
    [
        new("default", "With sidebar", typeof(AuroraLayout)) { Description = "Content with the sidebar on the right." },
        new("full-width", "Full width", typeof(AuroraFullWidthLayout)) { Description = "Content across the whole page, no sidebar." },
    ];

    public IReadOnlyList<ThemeSettingDefinition> Settings { get; } =
    [
        new()
        {
            Key = "accent",
            Name = "Accent colour",
            Kind = ThemeSettingKind.Color,
            DefaultValue = "#0f766e",
            CssVariable = "--aurora-accent",
        },
        new()
        {
            Key = "tagline",
            Name = "Tagline",
            DefaultValue = "Notes from the studio",
            Description = "Shown under the site name.",
            MaxLength = 80,
        },
        new()
        {
            Key = "rounded",
            Name = "Rounded corners",
            Kind = ThemeSettingKind.Boolean,
            DefaultValue = "true",
        },
    ];
}
