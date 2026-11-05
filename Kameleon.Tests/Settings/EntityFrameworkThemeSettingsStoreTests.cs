using Kameleon.Data;
using Kameleon.Data.Sqlite;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Kameleon.Tests.Settings;

/// <summary>Runs the services against a freshly migrated in-memory SQLite database.</summary>
public sealed class EntityFrameworkThemeSettingsStoreTests : IDisposable
{
    private readonly SqliteConnection keepAlive;
    private readonly string connectionString = $"Data Source=kameleon-tests-{Guid.NewGuid():N};Mode=Memory;Cache=Shared";

    public EntityFrameworkThemeSettingsStoreTests()
    {
        // The open connection keeps the shared in-memory database alive between contexts.
        keepAlive = new SqliteConnection(connectionString);
        keepAlive.Open();

        using var context = new KameleonDbContextFactory(connectionString).GetContext();
        context.Database.Migrate();
    }

    public void Dispose() => keepAlive.Dispose();

    private KameleonHarness Harness() => new(services: services => services.AddKameleonSqlite(connectionString));

    [Fact]
    public void Store_ProviderRegistered_IsEntityFramework()
    {
        using var harness = Harness();

        Assert.IsType<EntityFrameworkThemeSettingsStore>(harness.Get<IThemeSettingsStore>());
        Assert.Equal(Constants.DatabaseProviders.Sqlite, harness.Get<IKameleonDbContextFactory>().ProviderName);
    }

    [Fact]
    public async Task Changes_SurviveANewHarness()
    {
        using (var harness = Harness())
        {
            ResultAssert.Succeeded(await harness.Admin.SetDefaultAsync(new SetDefaultThemeRequest { ThemeId = "alpha", LayoutId = "wide" }));
            ResultAssert.Succeeded(await harness.Admin.SaveSettingsAsync(new SaveThemeSettingsRequest
            {
                ThemeId = "alpha",
                Values = new Dictionary<string, string?> { ["accent"] = "#abcdef" },
            }));
            ResultAssert.Succeeded(await harness.CatalogAdmin.SetAvailabilityAsync("beta", ThemeAvailability.SelectedTenants));
            ResultAssert.Succeeded(await harness.CatalogAdmin.SetGrantAsync("beta", "site-a", true));
            ResultAssert.Succeeded(await harness.UserThemes.SetAsync("beta"));
        }

        using var fresh = Harness();
        var context = await fresh.Resolver.ResolveAsync();
        fresh.User.UserId = "someone-else";
        var otherUser = await fresh.Resolver.ResolveAsync();
        var otherTenant = await fresh.AsTenantAsync("site-b", () => fresh.UserThemes.GetChoicesAsync());

        Assert.Equal("beta", context.ThemeId);
        Assert.Equal("alpha", otherUser.ThemeId);
        Assert.Equal("wide", otherUser.Layout?.Id);
        Assert.Equal("#abcdef", otherUser.Setting("accent"));
        Assert.DoesNotContain(otherTenant.Value.Themes, theme => theme.Id == "beta");
    }

    [Fact]
    public async Task SetUserThemeAsync_Null_RemovesChoice()
    {
        using var harness = Harness();
        ResultAssert.Succeeded(await harness.UserThemes.SetAsync("beta"));

        ResultAssert.Succeeded(await harness.UserThemes.SetAsync(null));

        Assert.Null(await harness.Get<IThemeSettingsStore>().GetUserThemeAsync("site-a", "user-1"));
        Assert.Equal("alpha", (await harness.Resolver.ResolveAsync()).ThemeId);
    }
}
