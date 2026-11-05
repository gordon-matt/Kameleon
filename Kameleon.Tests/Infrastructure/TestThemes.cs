namespace Kameleon.Tests.Infrastructure;

public sealed class TestLayout : LayoutComponentBase;

public sealed class TestWideLayout : LayoutComponentBase;

public sealed class TestView : ComponentBase;

/// <summary>Two layouts and two settings.</summary>
public sealed class AlphaTheme : ITheme
{
    public ThemeDescriptor Descriptor { get; } = new()
    {
        Id = "alpha",
        Name = "Alpha",
        Stylesheets = ["alpha.css"],
    };

    public IReadOnlyList<ThemeLayoutDescriptor> Layouts { get; } =
    [
        new("default", "Default", typeof(TestLayout)),
        new("wide", "Wide", typeof(TestWideLayout)),
    ];

    public IReadOnlyList<ThemeSettingDefinition> Settings { get; } =
    [
        new() { Key = "accent", Name = "Accent", Kind = ThemeSettingKind.Color, DefaultValue = "#112233", CssVariable = "--alpha-accent" },
        new() { Key = "tagline", Name = "Tagline", DefaultValue = "Hello", MaxLength = 20 },
    ];
}

public sealed class BetaTheme : ITheme
{
    public ThemeDescriptor Descriptor { get; } = new() { Id = "beta", Name = "Beta" };

    public IReadOnlyList<ThemeLayoutDescriptor> Layouts { get; } = [new("default", "Default", typeof(TestLayout))];

    public IReadOnlyDictionary<string, Type> Views { get; } = new Dictionary<string, Type> { ["test.view"] = typeof(TestView) };
}

public sealed class GammaTheme : ITheme
{
    public ThemeDescriptor Descriptor { get; } = new() { Id = "gamma", Name = "Gamma" };

    public IReadOnlyList<ThemeLayoutDescriptor> Layouts { get; } = [new("default", "Default", typeof(TestLayout))];
}
