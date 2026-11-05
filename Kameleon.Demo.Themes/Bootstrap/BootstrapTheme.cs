namespace Kameleon.Demo.Themes.Bootstrap;

/// <summary>
/// A theme built on Bootstrap. It shows that a theme brings its own CSS framework: this assembly references only
/// Kameleon.Abstractions, and the stylesheet is Bootstrap compiled under a <c>.bs-theme</c> scope so it cannot
/// restyle pages that use another layout.
/// </summary>
public sealed class BootstrapTheme : ITheme
{
    public ThemeDescriptor Descriptor { get; } = new()
    {
        Id = "bootstrap",
        Name = "Bootstrap",
        Version = "1.0.0",
        Description = "A Bootstrap 5 theme: navbar, hero, card sidebar and the standard grid.",
        Author = "Kameleon samples",
        PreviewImage = "bootstrap/preview.svg",
        Stylesheets = ["bootstrap/bootstrap-theme.css"],
        Scripts = ["bootstrap/bootstrap.bundle.min.js"],
        DefaultLayoutId = "default",
    };

    public IReadOnlyList<ThemeLayoutDescriptor> Layouts { get; } =
    [
        new("default", "Navbar and sidebar", typeof(BootstrapLayout)) { Description = "Content in a Bootstrap grid, with a card sidebar when the page has one." },
    ];

    public IReadOnlyList<ThemeSettingDefinition> Settings { get; } =
    [
        new()
        {
            Key = "accent",
            Name = "Accent colour",
            Description = "The navbar and the links.",
            Kind = ThemeSettingKind.Color,
            DefaultValue = "#0d6efd",
            CssVariable = "--bsx-accent",
        },
        new()
        {
            Key = "fluid",
            Name = "Full-width content",
            Description = "Use container-fluid instead of a fixed-width container.",
            Kind = ThemeSettingKind.Boolean,
            DefaultValue = "false",
        },
    ];
}
