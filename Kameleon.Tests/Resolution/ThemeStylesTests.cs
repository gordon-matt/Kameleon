namespace Kameleon.Tests.Resolution;

public sealed class ThemeStylesTests
{
    private static ThemeContext Context(IReadOnlyList<ThemeSettingDefinition> definitions, Dictionary<string, string> values) => new()
    {
        TenantId = string.Empty,
        Theme = new ThemeInfo
        {
            Id = "test",
            Name = "Test",
            Version = "1.0.0",
            Kind = ThemeKind.Compiled,
            AssetBasePath = string.Empty,
            Source = "test",
            Settings = definitions,
        },
        Settings = values,
    };

    [Fact]
    public void BuildRootVariables_OnlyEmitsValidColours()
    {
        var context = Context(
            [
                new() { Key = "good", Name = "Good", Kind = ThemeSettingKind.Color, CssVariable = "--good" },
                new() { Key = "bad", Name = "Bad", Kind = ThemeSettingKind.Color, CssVariable = "--bad" },
                new() { Key = "text", Name = "Text", CssVariable = "--text" },
            ],
            new() { ["good"] = "#0f766e", ["bad"] = "red;}body{display:none", ["text"] = "#000" });

        Assert.Equal(":root{--good:#0f766e;}", ThemeStyles.BuildRootVariables(context));
    }

    [Fact]
    public void BuildRootVariables_NoTheme_ReturnsNull()
    {
        Assert.Null(ThemeStyles.BuildRootVariables(ThemeContext.None(string.Empty, null)));
    }
}
