namespace Kameleon.Tests.Administration;

public sealed class AdminServiceTests
{
    [Fact]
    public async Task SetDefaultAsync_WithoutPermission_ReturnsForbidden()
    {
        using var harness = new KameleonHarness();
        harness.Authorization.CanManage = false;

        var result = await harness.Admin.SetDefaultAsync(new SetDefaultThemeRequest { ThemeId = "beta" });

        Assert.Equal(ResultStatus.Forbidden, result.Status);
        Assert.Equal("alpha", (await harness.Resolver.ResolveAsync()).ThemeId);
    }

    [Fact]
    public async Task GetAsync_WithoutPermission_ReturnsForbidden()
    {
        using var harness = new KameleonHarness();
        harness.Authorization.CanManage = false;

        Assert.Equal(ResultStatus.Forbidden, (await harness.Admin.GetAsync()).Status);
    }

    [Fact]
    public async Task CatalogAdmin_TenantManagerOnly_ReturnsForbidden()
    {
        using var harness = new KameleonHarness();
        harness.Authorization.CanManageCatalog = false;

        Assert.Equal(ResultStatus.Forbidden, (await harness.CatalogAdmin.GetAsync()).Status);
        Assert.Equal(ResultStatus.Forbidden, (await harness.CatalogAdmin.SetEnabledAsync("beta", false)).Status);
        Assert.Equal(ResultStatus.Forbidden, (await harness.CatalogAdmin.SetGrantAsync("beta", "site-b", true)).Status);
    }

    [Fact]
    public async Task SetDefaultAsync_UnknownLayout_ReturnsInvalid()
    {
        using var harness = new KameleonHarness();

        var result = await harness.Admin.SetDefaultAsync(new SetDefaultThemeRequest { ThemeId = "alpha", LayoutId = "missing" });

        Assert.Equal(ResultStatus.Invalid, result.Status);
    }

    [Fact]
    public async Task SetDefaultAsync_ThemeNotEnabled_EnablesIt()
    {
        using var harness = new KameleonHarness(services: services => services.AddKameleonTheme<GammaTheme>());

        ResultAssert.Succeeded(await harness.Admin.SetDefaultAsync(new SetDefaultThemeRequest { ThemeId = "gamma" }));
        var settings = await harness.Admin.GetAsync();

        var gamma = Assert.Single(settings.Value.Themes, theme => theme.Theme.Id == "gamma");
        Assert.True(gamma.IsEnabled);
        Assert.True(gamma.IsDefault);
    }

    [Fact]
    public async Task SetEnabledAsync_DefaultTheme_ReturnsInvalid()
    {
        using var harness = new KameleonHarness();

        var result = await harness.Admin.SetEnabledAsync("alpha", false);

        Assert.Equal(ResultStatus.Invalid, result.Status);
    }

    [Fact]
    public async Task SetEnabledAsync_NewRecord_DoesNotChangeOtherThemes()
    {
        using var harness = new KameleonHarness();

        ResultAssert.Succeeded(await harness.Admin.SaveSettingsAsync(new SaveThemeSettingsRequest
        {
            ThemeId = "beta",
            Values = new Dictionary<string, string?>(),
        }));
        var settings = await harness.Admin.GetAsync();

        Assert.True(Assert.Single(settings.Value.Themes, theme => theme.Theme.Id == "beta").IsEnabled);
    }

    [Theory]
    [InlineData("accent", "red;}body{display:none")]
    [InlineData("accent", "#12345")]
    [InlineData("tagline", "This tagline is far too long to be accepted")]
    [InlineData("tagline", "Line\nbreak")]
    [InlineData("unknown", "value")]
    public async Task SaveSettingsAsync_InvalidValue_ReturnsInvalidAndSavesNothing(string key, string value)
    {
        using var harness = new KameleonHarness();

        var result = await harness.Admin.SaveSettingsAsync(new SaveThemeSettingsRequest
        {
            ThemeId = "alpha",
            Values = new Dictionary<string, string?> { [key] = value },
        });

        Assert.Equal(ResultStatus.Invalid, result.Status);
        Assert.Contains(result.ValidationErrors, error => error.Identifier == key);
        Assert.Equal("#112233", (await harness.Resolver.ResolveAsync()).Setting("accent"));
    }

    [Fact]
    public async Task SaveSettingsAsync_EmptyValue_ReturnsSettingToDefault()
    {
        using var harness = new KameleonHarness();
        ResultAssert.Succeeded(await harness.Admin.SaveSettingsAsync(new SaveThemeSettingsRequest
        {
            ThemeId = "alpha",
            Values = new Dictionary<string, string?> { ["tagline"] = "Custom" },
        }));

        ResultAssert.Succeeded(await harness.Admin.SaveSettingsAsync(new SaveThemeSettingsRequest
        {
            ThemeId = "alpha",
            Values = new Dictionary<string, string?> { ["tagline"] = " " },
        }));

        Assert.Equal("Hello", (await harness.Resolver.ResolveAsync()).Setting("tagline"));
    }

    [Fact]
    public async Task SaveSettingsAsync_OtherTenant_IsNotAffected()
    {
        using var harness = new KameleonHarness();
        ResultAssert.Succeeded(await harness.Admin.SaveSettingsAsync(new SaveThemeSettingsRequest
        {
            ThemeId = "alpha",
            Values = new Dictionary<string, string?> { ["accent"] = "#abcdef" },
        }));

        var otherTenant = await harness.AsTenantAsync("site-b", () => harness.Resolver.ResolveAsync());

        Assert.Equal("#112233", otherTenant.Setting("accent"));
    }

    [Fact]
    public async Task CatalogSetEnabledAsync_FallbackTheme_ReturnsInvalid()
    {
        using var harness = new KameleonHarness();

        var result = await harness.CatalogAdmin.SetEnabledAsync("alpha", false);

        Assert.Equal(ResultStatus.Invalid, result.Status);
    }

    [Fact]
    public async Task GetGrantsAsync_WithDirectory_ListsTenantsWithGrants()
    {
        using var harness = new KameleonHarness(services: services => services.AddSingleton<IThemeTenantDirectory, TestTenantDirectory>());
        ResultAssert.Succeeded(await harness.CatalogAdmin.SetGrantAsync("beta", "site-b", true));

        var grants = await harness.CatalogAdmin.GetGrantsAsync("beta");

        Assert.Collection(grants.Value,
            grant => Assert.False(grant.IsGranted),
            grant => Assert.True(grant.IsGranted));
    }

    [Fact]
    public async Task SetGrantAsync_UnknownTenant_ReturnsNotFound()
    {
        using var harness = new KameleonHarness(services: services => services.AddSingleton<IThemeTenantDirectory, TestTenantDirectory>());

        var result = await harness.CatalogAdmin.SetGrantAsync("beta", "site-z", true);

        Assert.Equal(ResultStatus.NotFound, result.Status);
    }

    [Fact]
    public async Task UserSetAsync_ThemeNotOffered_ReturnsInvalid()
    {
        using var harness = new KameleonHarness(services: services => services.AddKameleonTheme<GammaTheme>());

        var result = await harness.UserThemes.SetAsync("gamma");

        Assert.Equal(ResultStatus.Invalid, result.Status);
    }

    [Fact]
    public async Task UserSetAsync_Anonymous_ReturnsForbidden()
    {
        using var harness = new KameleonHarness();
        harness.User.UserId = null;

        Assert.Equal(ResultStatus.Forbidden, (await harness.UserThemes.SetAsync("beta")).Status);
    }

    [Fact]
    public async Task GetChoicesAsync_OverrideAllowedAndSeveralThemes_CanChoose()
    {
        using var harness = new KameleonHarness();

        var choices = await harness.UserThemes.GetChoicesAsync();

        Assert.True(choices.Value.CanChoose);
        Assert.Equal(["alpha", "beta"], choices.Value.Themes.Select(theme => theme.Id).Order());
    }

    [Fact]
    public async Task GetChoicesAsync_OverrideNotAllowed_CannotChoose()
    {
        using var harness = new KameleonHarness(options => options.AllowUserOverride = false);

        Assert.False((await harness.UserThemes.GetChoicesAsync()).Value.CanChoose);
    }
}
