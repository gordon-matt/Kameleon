namespace Kameleon.Tests.Resolution;

public sealed class ThemeResolverTests
{
    [Fact]
    public async Task ResolveAsync_NoSavedSettings_UsesApplicationDefault()
    {
        using var harness = new KameleonHarness();

        var context = await harness.Resolver.ResolveAsync();

        Assert.Equal("alpha", context.ThemeId);
        Assert.Equal("default", context.Layout?.Id);
        Assert.Equal("site-a", context.TenantId);
        Assert.False(context.IsUserOverride);
    }

    [Fact]
    public async Task ResolveAsync_TenantDefault_BeatsApplicationDefault()
    {
        using var harness = new KameleonHarness();
        ResultAssert.Succeeded(await harness.Admin.SetDefaultAsync(new SetDefaultThemeRequest { ThemeId = "beta" }));

        var context = await harness.Resolver.ResolveAsync();

        Assert.Equal("beta", context.ThemeId);
    }

    [Fact]
    public async Task ResolveAsync_TenantLayout_IsUsed()
    {
        using var harness = new KameleonHarness();
        ResultAssert.Succeeded(await harness.Admin.SetDefaultAsync(new SetDefaultThemeRequest { ThemeId = "alpha", LayoutId = "wide" }));

        var context = await harness.Resolver.ResolveAsync();

        Assert.Equal(typeof(TestWideLayout), context.Layout?.ComponentType);
    }

    [Fact]
    public async Task ResolveAsync_UnknownApplicationLayout_UsesThemeDefaultLayout()
    {
        using var harness = new KameleonHarness(options => options.DefaultLayoutId = "missing");

        var context = await harness.Resolver.ResolveAsync();

        Assert.Equal("default", context.Layout?.Id);
    }

    [Fact]
    public async Task ResolveAsync_UserChoice_BeatsTenantDefault()
    {
        using var harness = new KameleonHarness();
        ResultAssert.Succeeded(await harness.UserThemes.SetAsync("beta"));

        var context = await harness.Resolver.ResolveAsync();

        Assert.Equal("beta", context.ThemeId);
        Assert.True(context.IsUserOverride);
    }

    [Fact]
    public async Task ResolveAsync_UserOverrideTurnedOff_IgnoresSavedChoice()
    {
        using var harness = new KameleonHarness();
        ResultAssert.Succeeded(await harness.UserThemes.SetAsync("beta"));
        ResultAssert.Succeeded(await harness.Admin.SetAllowUserOverrideAsync(false));

        var context = await harness.Resolver.ResolveAsync();

        Assert.Equal("alpha", context.ThemeId);
    }

    [Fact]
    public async Task ResolveAsync_UserChoiceNoLongerOffered_FallsBackToTenantDefault()
    {
        using var harness = new KameleonHarness();
        ResultAssert.Succeeded(await harness.UserThemes.SetAsync("beta"));
        ResultAssert.Succeeded(await harness.Admin.SetEnabledAsync("beta", false));

        var context = await harness.Resolver.ResolveAsync();

        Assert.Equal("alpha", context.ThemeId);
    }

    [Fact]
    public async Task ResolveAsync_CachedChoice_IsNotServedToAnotherUser()
    {
        using var harness = new KameleonHarness();
        ResultAssert.Succeeded(await harness.UserThemes.SetAsync("beta"));
        Assert.Equal("beta", (await harness.Resolver.ResolveAsync()).ThemeId);

        harness.User.UserId = "user-2";
        var other = await harness.Resolver.ResolveAsync();

        Assert.Equal("alpha", other.ThemeId);
        Assert.Equal("user-2", other.UserId);
    }

    [Fact]
    public async Task ResolveAsync_SameUserInTwoTenants_KeepsChoicesApart()
    {
        using var harness = new KameleonHarness();
        ResultAssert.Succeeded(await harness.UserThemes.SetAsync("beta"));

        var otherTenant = await harness.AsTenantAsync("site-b", () => harness.Resolver.ResolveAsync());

        Assert.Equal("alpha", otherTenant.ThemeId);
        Assert.Equal("beta", (await harness.Resolver.ResolveAsync()).ThemeId);
    }

    [Fact]
    public async Task ResolveAsync_ThemeGrantedToOneTenant_IsNotUsedByAnother()
    {
        using var harness = new KameleonHarness(services: services => services.AddKameleonTheme<GammaTheme>());
        ResultAssert.Succeeded(await harness.CatalogAdmin.SetAvailabilityAsync("gamma", ThemeAvailability.SelectedTenants));
        ResultAssert.Succeeded(await harness.CatalogAdmin.SetGrantAsync("gamma", "site-a", true));
        ResultAssert.Succeeded(await harness.Admin.SetDefaultAsync(new SetDefaultThemeRequest { ThemeId = "gamma" }));

        var refused = await harness.AsTenantAsync("site-b", () => harness.Admin.SetDefaultAsync(new SetDefaultThemeRequest { ThemeId = "gamma" }));
        var otherTenant = await harness.AsTenantAsync("site-b", () => harness.Resolver.ResolveAsync());

        Assert.Equal("gamma", (await harness.Resolver.ResolveAsync()).ThemeId);
        Assert.Equal(ResultStatus.NotFound, refused.Status);
        Assert.Equal("alpha", otherTenant.ThemeId);
    }

    [Fact]
    public async Task ResolveAsync_GrantRevoked_TenantFallsBack()
    {
        using var harness = new KameleonHarness(services: services => services.AddKameleonTheme<GammaTheme>());
        ResultAssert.Succeeded(await harness.CatalogAdmin.SetAvailabilityAsync("gamma", ThemeAvailability.SelectedTenants));
        ResultAssert.Succeeded(await harness.CatalogAdmin.SetGrantAsync("gamma", "site-a", true));
        ResultAssert.Succeeded(await harness.Admin.SetDefaultAsync(new SetDefaultThemeRequest { ThemeId = "gamma" }));

        ResultAssert.Succeeded(await harness.CatalogAdmin.SetGrantAsync("gamma", "site-a", false));
        var settings = await harness.Admin.GetAsync();

        Assert.Equal("alpha", (await harness.Resolver.ResolveAsync()).ThemeId);
        Assert.True(settings.Value.DefaultIsUnavailable);
    }

    [Fact]
    public async Task ResolveAsync_DefaultDisabledGlobally_UsesFallback()
    {
        using var harness = new KameleonHarness(options => options.DefaultThemeId = "beta");
        ResultAssert.Succeeded(await harness.CatalogAdmin.SetEnabledAsync("beta", false));

        var context = await harness.Resolver.ResolveAsync();

        Assert.Equal("alpha", context.ThemeId);
    }

    [Fact]
    public async Task ResolveAsync_NothingResolves_ReturnsNoTheme()
    {
        using var harness = new KameleonHarness(options =>
        {
            options.FallbackThemeId = null;
            options.DefaultThemeId = "missing";
        });

        var context = await harness.Resolver.ResolveAsync();

        Assert.Null(context.Theme);
        Assert.Null(context.Layout);
    }

    [Fact]
    public async Task ResolveAsync_StoreFails_UsesFallbackInsteadOfThrowing()
    {
        var store = new Mock<IThemeSettingsStore>();
        store.Setup(item => item.GetCatalogAsync(It.IsAny<CancellationToken>())).ThrowsAsync(new InvalidOperationException("Database down"));
        using var harness = new KameleonHarness(options => options.DefaultThemeId = "beta", services => services.AddScoped(_ => store.Object));

        var context = await harness.Resolver.ResolveAsync();

        Assert.Equal("alpha", context.ThemeId);
    }

    [Fact]
    public async Task ResolveAsync_SavedSettings_AreMergedOverDefaults()
    {
        using var harness = new KameleonHarness();
        ResultAssert.Succeeded(await harness.Admin.SaveSettingsAsync(new SaveThemeSettingsRequest
        {
            ThemeId = "alpha",
            Values = new Dictionary<string, string?> { ["accent"] = "#abcdef" },
        }));

        var context = await harness.Resolver.ResolveAsync();

        Assert.Equal("#abcdef", context.Setting("accent"));
        Assert.Equal("Hello", context.Setting("tagline"));
        Assert.Equal(":root{--alpha-accent:#abcdef;}", ThemeStyles.BuildRootVariables(context));
    }
}
