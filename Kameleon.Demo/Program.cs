using Heroicsoft.NetForge.Components;
using Heroicsoft.NetForge.Components.Dialogs;
using Heroicsoft.NetForge.Components.Layout;
using Kameleon;
using Kameleon.Data.Sqlite;
using Kameleon.Demo.Components;
using Kameleon.Demo.Components.Layout;
using Kameleon.Demo.Infrastructure;
using Kameleon.Demo.Themes.Atlas;
using Kameleon.Demo.Themes.Aurora;
using Kameleon.Demo.Themes.Bootstrap;
using Kameleon.Tenancy;
using Microsoft.AspNetCore.Authentication;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddAuthentication(DemoAuthenticationHandler.SchemeName)
    .AddScheme<AuthenticationSchemeOptions, DemoAuthenticationHandler>(DemoAuthenticationHandler.SchemeName, null);
builder.Services.AddAuthorization();
builder.Services.AddHttpContextAccessor();

builder.Services.AddNetForgeComponents(ui =>
{
    ui.ThemeStorageKey = "kameleon-demo.scheme";
    ui.DefaultColorScheme = "system";
    ui.AccentColor = "#0f766e";
    ui.AccentColorDark = "#2dd4bf";
});

builder.Services.ConfigureNetForgeToastSounds(sounds => sounds.EnableSounds = true);

builder.Services.Configure<ForgeShellOptions>(shell =>
{
    shell.Brand = "Kameleon";
    shell.BrandHref = "/";
    shell.LogoUrl = "img/Logo.png";
    shell.MarkUrl = "img/Icon.png";
    shell.UseSharedMenu = true;
});

builder.Services.BuildMenu(menu => menu
    .AsSidebar()
    .AddMenu("Appearance")
    .AddKameleon());

// Two demo sites, chosen with a cookie, so that granting a theme to one site can be tried out.
builder.Services.AddScoped<IThemeTenantProvider, DemoSiteProvider>();
builder.Services.AddSingleton<IThemeTenantDirectory, DemoSiteProvider.Directory>();

builder.Services.AddKameleonTheme<AuroraTheme>();
builder.Services.AddKameleonTheme<AtlasTheme>();
builder.Services.AddKameleonTheme<BootstrapTheme>();

builder.Services.AddKameleon(options =>
{
    builder.Configuration.GetSection(KameleonOptions.SectionName).Bind(options);
    options.HostLayout = typeof(DemoHostLayout);
    options.AddRegionContent<DemoBrand>(ThemeRegions.Header);
    options.AddRegionContent<DemoNavigation>(ThemeRegions.PrimaryNavigation);
    options.AddRegionContent<ForgeDialogHost>(ThemeRegions.Overlay);
});
builder.Services.AddKameleonSqlite(builder.Configuration.GetConnectionString("Kameleon"));

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/error", createScopeForErrors: true);
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.MapStaticAssets();
app.UseAntiforgery();

app.UseAuthentication();
app.UseAuthorization();

app.MapKameleon();
app.MapDemoSites();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddAdditionalAssemblies(KameleonComponents.Assembly);

app.Run();
