using Kameleon.FileSystem;

namespace Kameleon.Tests.Catalog;

public sealed class ThemeCatalogTests
{
    private sealed class FixedSource(params ThemeCandidate[] candidates) : IThemeSource
    {
        public ThemeSourceResult Load() => new(candidates, []);
    }

    private static ThemeCandidate Candidate(string id, string version, ThemeKind kind = ThemeKind.Compiled, string source = "test") => new()
    {
        Source = source,
        Kind = kind,
        Descriptor = new ThemeDescriptor { Id = id, Name = id, Version = version },
        Layouts = kind == ThemeKind.Compiled ? [new("default", "Default", typeof(TestLayout))] : [],
        AssetBasePath = "_content/Test/",
    };

    private static ThemeCatalog Build(params IThemeSource[] sources) =>
        new(sources, Options.Create(new KameleonOptions()), NullLogger<ThemeCatalog>.Instance);

    [Fact]
    public void Themes_DuplicateId_HighestVersionWinsAndLoserIsReported()
    {
        var catalog = Build(new FixedSource(Candidate("dup", "1.2.0", source: "old"), Candidate("dup", "1.10.0", source: "new")));

        var theme = Assert.Single(catalog.Themes);
        Assert.Equal("1.10.0", theme.Version);
        Assert.Equal("new", theme.Source);
        Assert.Contains(catalog.Errors, error => error.ThemeId == "dup" && error.Source == "old");
    }

    [Fact]
    public void Themes_DuplicateIdAndVersion_CompiledWinsOverStatic()
    {
        var catalog = Build(new FixedSource(Candidate("dup", "1.0.0", ThemeKind.Static, "folder"), Candidate("dup", "1.0.0")));

        Assert.Equal(ThemeKind.Compiled, Assert.Single(catalog.Themes).Kind);
    }

    [Fact]
    public void Themes_InvalidTheme_IsRejectedWithError()
    {
        var catalog = Build(new FixedSource(Candidate("Not Valid", "1.0.0"), Candidate("good", "1.0.0")));

        Assert.Equal("good", Assert.Single(catalog.Themes).Id);
        Assert.Contains(catalog.Errors, error => error.ThemeId == "Not Valid");
    }

    [Fact]
    public void Find_CompiledTheme_ResolvesAssetUrls()
    {
        var catalog = Build(new CompiledThemeSource([new AlphaTheme()]));

        var alpha = catalog.Find("alpha");

        Assert.NotNull(alpha);
        Assert.Equal(["_content/Kameleon.Tests/alpha.css"], alpha.StylesheetUrls);
        Assert.Equal("default", alpha.DefaultLayout?.Id);
    }

    [Fact]
    public void Load_StaticThemeFolder_RegistersThemeAndRejectsMalformedManifest()
    {
        string root = Path.Combine(Path.GetTempPath(), "kameleon-tests-" + Guid.NewGuid().ToString("N"));
        try
        {
            Directory.CreateDirectory(Path.Combine(root, "Themes", "harbour"));
            File.WriteAllText(Path.Combine(root, "Themes", "harbour", "theme.json"), """
                {
                  // Comments and trailing commas are allowed.
                  "schemaVersion": 1,
                  "name": "Harbour",
                  "version": "2.0.0",
                  "stylesheets": [ "harbour.css" ],
                  "settings": [ { "key": "accent", "name": "Accent", "kind": "color", "default": "#123456", "cssVariable": "--harbour-accent" } ],
                }
                """);
            Directory.CreateDirectory(Path.Combine(root, "Themes", "broken"));
            File.WriteAllText(Path.Combine(root, "Themes", "broken", "theme.json"), "{ not json");

            var options = Options.Create(new KameleonOptions());
            var catalog = Build(new FileSystemThemeSource(new TestHostEnvironment(root), options));

            var harbour = Assert.Single(catalog.Themes);
            Assert.Equal("harbour", harbour.Id);
            Assert.Equal(ThemeKind.Static, harbour.Kind);
            Assert.Equal(["themes/harbour/2.0.0/harbour.css"], harbour.StylesheetUrls);
            Assert.Equal("Themes/harbour", harbour.Source);
            Assert.Equal(Path.Combine(root, "Themes", "harbour"), catalog.GetContentRoot("harbour"));
            Assert.Contains(catalog.Errors, error => error.Source == "Themes/broken");
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Theory]
    [InlineData("1.0.0", "1.0.0-beta", 1)]
    [InlineData("1.10.0", "1.9.0", 1)]
    [InlineData("2.0.0-alpha", "2.0.0-beta", -1)]
    [InlineData("1.0.0", "1.0.0", 0)]
    public void CompareTo_Versions_OrdersSemantically(string left, string right, int expected)
    {
        Assert.True(ThemeVersion.TryParse(left, out var a));
        Assert.True(ThemeVersion.TryParse(right, out var b));

        Assert.Equal(expected, Math.Sign(a.CompareTo(b)));
    }
}
