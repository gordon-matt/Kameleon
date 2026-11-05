namespace Kameleon.Tests.Infrastructure;

/// <summary>
/// Kameleon's services as a host registers them, with the Alpha and Beta themes, an in-memory store unless a
/// test adds a database, and a tenant, user and authorization the test controls.
/// </summary>
public sealed class KameleonHarness : IDisposable
{
    private readonly ServiceProvider provider;
    private readonly IServiceScope scope;

    public KameleonHarness(Action<KameleonOptions>? configure = null, Action<IServiceCollection>? services = null)
    {
        var collection = new ServiceCollection();
        collection.AddLogging();
        collection.AddSingleton<Microsoft.Extensions.Hosting.IHostEnvironment>(new TestHostEnvironment(AppContext.BaseDirectory));
        collection.AddSingleton<IThemeTenantProvider>(Tenant);
        collection.AddSingleton<IUserContextService>(User);
        collection.AddSingleton<IThemeAuthorization>(Authorization);
        collection.AddKameleonTheme<AlphaTheme>();
        collection.AddKameleonTheme<BetaTheme>();
        services?.Invoke(collection);

        collection.AddKameleonServices(options =>
        {
            options.ThemesPath = null;
            options.FallbackThemeId = "alpha";
            options.DefaultThemeId = "alpha";
            options.EnabledThemes = ["alpha", "beta"];
            options.AllowUserOverride = true;
            configure?.Invoke(options);
        });

        provider = collection.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        scope = provider.CreateScope();
    }

    public TestTenant Tenant { get; } = new();

    public TestUser User { get; } = new();

    public TestAuthorization Authorization { get; } = new();

    public IThemeResolver Resolver => Get<IThemeResolver>();

    public IThemeAdminService Admin => Get<IThemeAdminService>();

    public IThemeCatalogAdminService CatalogAdmin => Get<IThemeCatalogAdminService>();

    public IUserThemeService UserThemes => Get<IUserThemeService>();

    public T Get<T>() where T : notnull => scope.ServiceProvider.GetRequiredService<T>();

    /// <summary>Runs <paramref name="action"/> as <paramref name="tenantId"/>, then switches back.</summary>
    public async Task<T> AsTenantAsync<T>(string tenantId, Func<Task<T>> action)
    {
        string previous = Tenant.Id;
        Tenant.Id = tenantId;
        try
        {
            return await action();
        }
        finally
        {
            Tenant.Id = previous;
        }
    }

    public void Dispose()
    {
        scope.Dispose();
        provider.Dispose();
    }
}

public static class ResultAssert
{
    public static void Succeeded(Ardalis.Result.IResult result) =>
        Assert.True(result.Status == ResultStatus.Ok, $"{result.Status}: {string.Join("; ", result.Errors.Concat(result.ValidationErrors.Select(error => error.ErrorMessage)))}");
}
