namespace Kameleon.Tests.Catalog;

public sealed class ThemeValidatorTests
{
    private static ThemeCandidate Compiled(ThemeDescriptor? descriptor = null, IReadOnlyList<ThemeLayoutDescriptor>? layouts = null, IReadOnlyList<ThemeSettingDefinition>? settings = null) => new()
    {
        Source = "test",
        Kind = ThemeKind.Compiled,
        Descriptor = descriptor ?? new ThemeDescriptor { Id = "valid", Name = "Valid" },
        Layouts = layouts ?? [new("default", "Default", typeof(TestLayout))],
        Settings = settings ?? [],
        AssetBasePath = "_content/Test/",
    };

    private static ThemeCandidate Static(ThemeDescriptor descriptor) => new()
    {
        Source = "Themes/test",
        Kind = ThemeKind.Static,
        Descriptor = descriptor,
        AssetBasePath = "themes/test/1.0.0/",
        ContentRoot = "C:/themes/test",
    };

    [Fact]
    public void Validate_ValidCompiledTheme_ReturnsNoErrors()
    {
        Assert.Empty(ThemeValidator.Validate(Compiled(), new KameleonOptions()));
    }

    [Theory]
    [InlineData("Upper")]
    [InlineData("has space")]
    [InlineData("double--hyphen")]
    [InlineData("-leading")]
    [InlineData("")]
    public void Validate_InvalidId_ReturnsError(string id)
    {
        var errors = ThemeValidator.Validate(Compiled(new ThemeDescriptor { Id = id, Name = "Bad" }), new KameleonOptions());

        Assert.NotEmpty(errors);
    }

    [Theory]
    [InlineData("../secret.css")]
    [InlineData("css/../../secret.css")]
    [InlineData("/etc/passwd")]
    [InlineData("C:\\windows\\win.ini")]
    [InlineData("javascript:alert(1)")]
    [InlineData("//evil.example/x.css")]
    public void Validate_UnsafeStylesheetPath_ReturnsError(string path)
    {
        var errors = ThemeValidator.Validate(Compiled(new ThemeDescriptor { Id = "valid", Name = "Valid", Stylesheets = [path] }), new KameleonOptions());

        Assert.NotEmpty(errors);
    }

    [Fact]
    public void Validate_HttpsStylesheet_AllowedForCompiledThemeOnly()
    {
        var descriptor = new ThemeDescriptor { Id = "valid", Name = "Valid", Stylesheets = ["https://cdn.example.com/x.css"] };

        Assert.Empty(ThemeValidator.Validate(Compiled(descriptor), new KameleonOptions()));
        Assert.NotEmpty(ThemeValidator.Validate(Static(descriptor), new KameleonOptions()));
    }

    [Fact]
    public void Validate_StaticThemeWithScripts_RejectedUnlessAllowed()
    {
        var candidate = Static(new ThemeDescriptor { Id = "valid", Name = "Valid", Scripts = ["theme.js"] });

        Assert.NotEmpty(ThemeValidator.Validate(candidate, new KameleonOptions()));
        Assert.Empty(ThemeValidator.Validate(candidate, new KameleonOptions { AllowStaticThemeScripts = true }));
    }

    [Fact]
    public void Validate_LayoutThatIsNotALayoutComponent_ReturnsError()
    {
        var errors = ThemeValidator.Validate(Compiled(layouts: [new("default", "Default", typeof(TestView))]), new KameleonOptions());

        Assert.NotEmpty(errors);
    }

    [Fact]
    public void Validate_CompiledThemeWithoutLayouts_ReturnsError()
    {
        Assert.NotEmpty(ThemeValidator.Validate(Compiled(layouts: []), new KameleonOptions()));
    }

    [Fact]
    public void Validate_DefaultLayoutThatDoesNotExist_ReturnsError()
    {
        var errors = ThemeValidator.Validate(Compiled(new ThemeDescriptor { Id = "valid", Name = "Valid", DefaultLayoutId = "missing" }), new KameleonOptions());

        Assert.NotEmpty(errors);
    }

    [Fact]
    public void Validate_ColorSettingWithInvalidDefault_ReturnsError()
    {
        var errors = ThemeValidator.Validate(
            Compiled(settings: [new() { Key = "accent", Name = "Accent", Kind = ThemeSettingKind.Color, DefaultValue = "red;}" }]),
            new KameleonOptions());

        Assert.NotEmpty(errors);
    }

    [Fact]
    public void Validate_CssVariableOnTextSetting_ReturnsError()
    {
        var errors = ThemeValidator.Validate(
            Compiled(settings: [new() { Key = "tagline", Name = "Tagline", CssVariable = "--tagline" }]),
            new KameleonOptions());

        Assert.NotEmpty(errors);
    }
}
