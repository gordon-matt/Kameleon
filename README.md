<img src="https://github.com/gordon-matt/Kameleon/blob/master/_Misc/Logo.png" alt="Logo" width="350" />

A theme engine for Blazor web apps on .NET 10. A theme decides how a site looks and where page content goes. Pages say what they show, and the theme arranges it.

- **Compiled themes** are Razor class libraries with their own layouts, views and assets.
- **Static themes** are a folder with a `theme.json` and some CSS. They restyle the host's own layout with no code.
- **Per-site settings.** Each site (tenant) chooses its default theme and layout, which themes visitors may pick, and each theme's settings, such as an accent colour.
- **Per-user choice.** Signed-in users can choose their own theme when the site allows it.
- **Catalog control.** An application administrator can turn a theme off everywhere, or offer it only to selected sites.
- **Safe resolution.** The theme for a request is resolved on the server and cached. It never throws: when something is missing it falls back, and finally renders the host's own layout.

## Packages

| Package | Use |
| --- | --- |
| `Kameleon` | The engine, its components and admin screens. Add it to the web app. |
| `Kameleon.Abstractions` | What a theme needs: `ITheme`, `ThemeLayoutBase`, `ThemeRegion`, `AddKameleonTheme`. Reference only this from a theme library. |
| `Kameleon.Services` | The engine without the UI. Pulled in by `Kameleon`. |
| `Kameleon.Data` | Entities and the base `DbContext`. Pulled in by a storage provider. |
| `Kameleon.Data.Sql`, `Kameleon.Data.MySql`, `Kameleon.Data.Npgsql`, `Kameleon.Data.Sqlite` | Storage for settings: SQL Server, MySQL, PostgreSQL or SQLite. |

Without a storage provider, settings are kept in memory and are lost when the app restarts. That is fine for trying Kameleon out, but not for production.

The admin screens use [NetForge](https://www.nuget.org/packages/Heroicsoft.NetForge.Components) components.

## Getting started

### 1. Register the services

```csharp
builder.Services.AddKameleonTheme<MyTheme>();

builder.Services.AddKameleon(options =>
{
    builder.Configuration.GetSection(KameleonOptions.SectionName).Bind(options);
    options.HostLayout = typeof(MainLayout);
});
builder.Services.AddKameleonSqlServer(builder.Configuration.GetConnectionString("Kameleon"));
```

Use `AddKameleonSqlite`, `AddKameleonNpgsql` or `AddKameleonMySql` for the other databases. Migrations are applied at startup unless `AutoMigrate` is off.

### 2. Map the endpoints and the screens

```csharp
app.MapStaticAssets();
app.UseAntiforgery();
app.UseAuthentication();
app.UseAuthorization();

app.MapKameleon();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddAdditionalAssemblies(KameleonComponents.Assembly);
```

`MapKameleon` serves the assets of static themes. `KameleonComponents.Assembly` contains the admin pages at `/admin/themes` and `/admin/themes/catalog`. Add the same assembly to the router.

### 3. Use the themed layout

In `Routes.razor`:

```razor
<Router AppAssembly="typeof(Program).Assembly" AdditionalAssemblies="new[] { KameleonComponents.Assembly }">
    <Found Context="routeData">
        <RouteView RouteData="routeData" DefaultLayout="typeof(KameleonLayout)" />
    </Found>
</Router>
```

`KameleonLayout` renders the layout of the resolved theme. When no theme resolves, it renders `HostLayout`, and when that is null it renders a plain built-in layout. Pages with their own `@layout` are not themed, which suits admin areas.

### 4. Add the head component

In `App.razor`, inside `<head>`:

```razor
<ThemeHead />
```

It links Kameleon's stylesheet, the theme's stylesheets and scripts, and a `<style>` element with the site's colour settings.

### 5. Configure

```json
{
  "Kameleon": {
    "FallbackThemeId": "my-theme",
    "DefaultThemeId": "my-theme",
    "EnabledThemes": [ "my-theme" ],
    "AllowUserOverride": false
  }
}
```

| Option | Default | Meaning |
| --- | --- | --- |
| `FallbackThemeId` | null | Used when a site's default is missing, disabled or not available to it. It cannot be disabled. |
| `DefaultThemeId`, `DefaultLayoutId` | null | The default for a site that has not chosen one. |
| `EnabledThemes` | empty | Themes offered to a site until its administrator changes that. A registered theme is offered to nobody unless it is listed here or a site turns it on. |
| `AllowUserOverride` | false | Whether signed-in users may choose their own theme, until a site decides. |
| `DefaultAvailability` | `AllTenants` | Whether a theme no administrator has configured is available to every site or only to selected ones. |
| `ThemesPath` | `Themes` | Folder of static themes, relative to the content root. Null turns static themes off. |
| `AssetRequestPath` | `/themes` | URL prefix for static theme assets. |
| `AllowStaticThemeScripts` | false | Whether static themes may load scripts. |
| `CacheDuration` | 5 minutes | How long resolved settings are cached. Zero turns caching off. |
| `AutoMigrate` | true | Apply migrations at startup. |
| `ManagerRoles` | `Administrator` | Roles that may manage a site's themes. |
| `CatalogManagerRoles` | `Administrator` | Roles that may manage the catalog. |
| `AdminPath` | `/admin/themes` | Where the admin screens live. |
| `AdminLayout` | null | Layout of the admin screens. Null uses the NetForge shell layout. |
| `HostLayout` | null | The host's own layout. Used by static themes, and when no theme resolves. |

## How a theme is chosen

For each request, the first of these that applies wins:

1. The signed-in user's own choice, if the site allows it and the theme is still eligible.
2. The site's default theme and layout.
3. `FallbackThemeId`.
4. No theme: pages render in `HostLayout`.

A theme is eligible for a site when:

- it is installed;
- it is not disabled in the catalog;
- it is available to every site, or the site has been granted it;
- the site has turned it on, or it is the site's default.

A missing or unknown layout falls back to the theme's default layout. A storage failure is logged and resolves to the fallback theme, so a database outage never takes the site down.

## Regions

A region is a named place in a layout. Pages fill regions and the theme decides where, or whether, each one appears. `ThemeRegions` defines the conventional names: `Header`, `PrimaryNavigation`, `Breadcrumbs`, `Hero`, `Sidebar`, `Aside`, `Footer` and `Overlay`. A theme may also define regions of its own.

A page supplies content with `RegionContent`:

```razor
<RegionContent Name="@ThemeRegions.Sidebar">
    <h2>Related</h2>
    <RelatedLinks />
</RegionContent>
```

The host can register components for regions that every page shares, such as its menu or a dialog host:

```csharp
builder.Services.AddKameleon(options =>
{
    options.AddRegionContent<SiteBrand>(ThemeRegions.Header);
    options.AddRegionContent<SiteMenu>(ThemeRegions.PrimaryNavigation);
    options.AddRegionContent<DialogHost>(ThemeRegions.Overlay);
});
```

A region shows the page's content first. If the page supplies none, it shows the host's components, and if there are none of those either, it shows the theme's own `ChildContent`.

## Theme views

A theme can only change what the host lets it change. A *view* is a named point where the host renders a component that a theme may replace:

```razor
<ThemeView Name="myapp.article-card" Fallback="typeof(ArticleCard)"
           Parameters="@(new Dictionary<string, object?> { ["Article"] = article })" />
```

Prefix view names with your application's name, and document the parameters each view receives. A theme replaces a view by listing it in `ITheme.Views`.

## Writing a compiled theme

Create a Razor class library that references `Kameleon.Abstractions`:

```csharp
public sealed class HarvestTheme : ITheme
{
    public ThemeDescriptor Descriptor { get; } = new()
    {
        Id = "harvest",
        Name = "Harvest",
        Version = "1.0.0",
        Description = "Warm colours and a sidebar on the right.",
        PreviewImage = "harvest/preview.png",
        Stylesheets = ["harvest/harvest.css"],
    };

    public IReadOnlyList<ThemeLayoutDescriptor> Layouts { get; } =
    [
        new("default", "With sidebar", typeof(HarvestLayout)),
        new("full-width", "Full width", typeof(HarvestFullWidthLayout)),
    ];

    public IReadOnlyDictionary<string, Type> Views { get; } = new Dictionary<string, Type>
    {
        ["myapp.article-card"] = typeof(HarvestArticleCard),
    };

    public IReadOnlyList<ThemeSettingDefinition> Settings { get; } =
    [
        new() { Key = "accent", Name = "Accent colour", Kind = ThemeSettingKind.Color, DefaultValue = "#b45309", CssVariable = "--harvest-accent" },
        new() { Key = "tagline", Name = "Tagline", DefaultValue = "Fresh every week", MaxLength = 80 },
    ];
}
```

Asset paths are relative to the library's `wwwroot`. They are served from `_content/{AssemblyName}/`. To build any other asset URL, call `Asset("harvest/logo.svg")` in a layout.

A layout derives from `ThemeLayoutBase`, which receives the current `ThemeContext`:

```razor
@inherits ThemeLayoutBase

<div class="harvest">
    <header class="harvest-header">
        <ThemeRegion Name="@ThemeRegions.Header" />
        <p>@Setting("tagline")</p>
        <ThemeRegion Name="@ThemeRegions.PrimaryNavigation" />
    </header>
    <div class="harvest-columns @(HasRegion(ThemeRegions.Sidebar) ? "has-sidebar" : null)">
        <main>@Body</main>
        <ThemeRegion Name="@ThemeRegions.Sidebar">
            <Template Context="content">
                <aside class="harvest-sidebar">@content</aside>
            </Template>
        </ThemeRegion>
    </div>
    <ThemeRegion Name="@ThemeRegions.Footer">© Harvest</ThemeRegion>
    <ThemeRegion Name="@ThemeRegions.Overlay" />
</div>
```

- A `Template` wraps the region's content only when there is some, so an empty sidebar leaves no empty `<aside>`.
- `HasRegion` lets a layout drop a column that has nothing in it.
- Render the `Overlay` region, because hosts put dialog and toast hosts there.

Register the theme from the host, or from a plugin while the host builds its services:

```csharp
services.AddKameleonTheme<HarvestTheme>();
```

Themes are read once, when the catalog is built. A theme added or removed by a plugin takes effect after a restart. A theme instance must not hold per-request state.

### Theme CSS

- **Declare colour defaults on `:root`.** `ThemeHead` writes the saved settings as `:root { --harvest-accent: …; }` after your stylesheet. A default declared on `:root` is overridden by the saved value. A default declared on a class such as `.harvest` would win over it, so the setting would never apply.

  ```css
  :root { --harvest-accent: #b45309; }
  .harvest-header { background: var(--harvest-accent); }
  ```

- **Scope everything else** under your layout's root class. `ThemeHead` loads the theme's CSS on every page that includes it, including pages with their own `@layout`, such as admin screens.
- **Follow the host's light and dark scheme** by building on the host's CSS variables rather than hard-coding a background.

## Writing a static theme

A static theme is a subfolder of `ThemesPath` that contains a `theme.json`:

```json
{
  "schemaVersion": 1,
  "id": "harbour",
  "name": "Harbour",
  "version": "1.0.0",
  "description": "A calm navy look for the site's own layout.",
  "author": "Example",
  "preview": "preview.svg",
  "stylesheets": [ "harbour.css" ],
  "scripts": [],
  "supportsRightToLeft": false,
  "settings": [
    {
      "key": "accent",
      "name": "Accent colour",
      "kind": "color",
      "default": "#1d4ed8",
      "cssVariable": "--harbour-accent"
    }
  ]
}
```

| Property | Required | Notes |
| --- | --- | --- |
| `schemaVersion` | yes | `1`. |
| `id` | no | Lowercase letters, digits and single hyphens. Defaults to the folder name in lowercase. |
| `name` | yes | |
| `version` | no | A semantic version. When two themes share an id, the higher version is used. |
| `preview`, `stylesheets`, `scripts` | no | Paths relative to the folder. `scripts` are rejected unless `AllowStaticThemeScripts` is on. |
| `settings` | no | `key`, `name`, `kind` (`text`, `color`, `boolean`, `choice` or `url`), `default`, `description`, `choices`, `cssVariable` and `maxLength`. |

Comments and trailing commas are allowed, and unknown properties are ignored. A static theme has no layouts: it restyles `HostLayout`, so its selectors are the host's own classes. Invalid themes are skipped and listed on the catalog screen, so one bad folder does not stop the others from loading. Changes to the folder are picked up with **Reload themes** on the catalog screen.

## Settings

A setting has a kind, and every value is checked against it when it is saved and again when it is loaded:

| Kind | Accepted values |
| --- | --- |
| `Text` | Any text up to `MaxLength`. Razor encodes it when rendered. |
| `Color` | `#rgb`, `#rrggbb` or `#rrggbbaa`. |
| `Boolean` | `true` or `false`. |
| `Choice` | One of `Choices`. |
| `Url` | A site-relative path starting with `/`, or an `http` or `https` address. |

Read values with `Theme.Setting("key")` or `Theme.IsSettingOn("key")`. Only colour settings can be written as CSS variables, because their format is strict enough to be safe inside a `<style>` element.

## Multiple sites (tenants)

Kameleon does not resolve tenants itself. It asks the host. A single-tenant site needs nothing. A multi-tenant host implements `IThemeTenantProvider`, which returns a stable key for the current tenant. Optionally, it also implements `IThemeTenantDirectory`, which lists tenants so that themes can be granted to selected sites:

```csharp
public sealed class TenantThemeAdapter(ICurrentTenant<Tenant> current) : IThemeTenantProvider
{
    public string GetTenantId() => current.Tenant?.Id.ToString() ?? string.Empty;
}

public sealed class TenantThemeDirectory(ITenantStore store) : IThemeTenantDirectory
{
    public async Task<IReadOnlyList<ThemeTenant>> ListAsync(CancellationToken cancellationToken = default) =>
        (await store.GetAllAsync(cancellationToken)).Select(tenant => new ThemeTenant(tenant.Id.ToString(), tenant.Name)).ToList();
}

builder.Services.AddScoped<IThemeTenantProvider, TenantThemeAdapter>();
builder.Services.AddScoped<IThemeTenantDirectory, TenantThemeDirectory>();
```

`GetTenantId` must also return the right tenant inside a Blazor circuit, where there is no `HttpContext`. Keys can be up to 64 characters long.

## Permissions

`AddKameleon` registers three policies. By default each one requires a signed-in user in the listed roles:

| Policy | Default roles | Allows |
| --- | --- | --- |
| `Kameleon.View` (`KameleonPolicies.View`) | `ManagerRoles` | Opening the site's theme screen. |
| `Kameleon.Manage` (`KameleonPolicies.Manage`) | `ManagerRoles` | Changing the current site's default theme, its offered themes, user choice and settings. |
| `Kameleon.ManageCatalog` (`KameleonPolicies.ManageCatalog`) | `CatalogManagerRoles` | Disabling themes everywhere, limiting them to selected sites, granting them, reloading the catalog. |

To use your own permission system, register a policy with the same name before or after calling `AddKameleon`. Kameleon leaves existing policies alone:

```csharp
builder.Services.AddAuthorizationBuilder()
    .AddPolicy(KameleonPolicies.View, policy => policy.RequireClaim("permission", "themes.read"))
    .AddPolicy(KameleonPolicies.Manage, policy => policy.RequireClaim("permission", "themes.write"))
    .AddPolicy(KameleonPolicies.ManageCatalog, policy => policy.RequireRole("PlatformAdmin"));
```

For full control, replace `IThemeAuthorization`. The services check permissions themselves, so a page that skips its `[Authorize]` attribute still cannot change anything.

## Admin screens and the menu

The ready-made pages are at `/admin/themes` (the site's themes) and `/admin/themes/catalog` (the catalog). They use `AdminLayout`, or the NetForge shell layout when that is null. To put them on a NetForge shared menu:

```csharp
builder.Services.BuildMenu(menu => menu
    .AsSidebar()
    .AddMenu("Appearance")
    .AddKameleon());
```

To host the screens somewhere else, set `AdminPath` and render the components from your own pages. The built-in pages then return 404. The screens are interactive, so give them an interactive render mode:

```razor
@page "/settings/appearance"
@rendermode InteractiveServer
@attribute [Authorize(Policy = KameleonPolicies.View)]

<ThemeSettingsScreen />
```

`ThemeCatalogScreen` is the catalog screen. `ThemePicker` is a small select that lets signed-in users choose their own theme. Put it anywhere, for example in a sidebar or an account menu.

## Caching and server farms

The settings of each site and each user's choice are cached for `CacheDuration`. Changes made through Kameleon's services clear the cache on that server at once, and open circuits re-render with the new theme. Other servers in a farm see the change when their cache expires. Shorten `CacheDuration` if that delay matters. If you change settings outside Kameleon's services, call `IThemeChangeNotifier`.

## Security notes

- The theme is resolved on the server. A visitor cannot pick a theme with a query string or a cookie.
- Colour values are validated strictly before they are written into a `<style>` element. Other setting kinds are never written into CSS.
- Static themes cannot run scripts unless `AllowStaticThemeScripts` is on, because anyone who can write to the themes folder could change them. Compiled themes are trusted code.
- `MapKameleon` serves only files inside a theme's folder whose types are known. It refuses `theme.json`, hidden files and path traversal, and sends `X-Content-Type-Options: nosniff`. Versioned asset URLs are cached as immutable.
- With a strict Content Security Policy, allow the inline `<style>` that `ThemeHead` writes for colour settings, or do not use `CssVariable`.

## Demo

`Kameleon.Demo` is a sample site with three compiled themes (Aurora, Atlas and Bootstrap), one static theme (Harbour) and two pretend sites, so tenant settings can be tried. Requests from the local machine are signed in as an administrator.

Themes do not depend on the UI library of the admin screens. A theme references only `Kameleon.Abstractions` and brings its own CSS framework. The Bootstrap theme in `Kameleon.Demo.Themes` shows how: its SCSS in `Styles/` compiles Bootstrap under a `.bs-theme` scope, so the framework's global resets cannot restyle other pages, and it loads Bootstrap's script through `ThemeDescriptor.Scripts`.

```
dotnet run --project Kameleon.Demo
```

## License

MIT
