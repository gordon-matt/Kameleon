# Kameleon — Theme Engine for .NET Blazor: Implementation Plan

## 0. How to use this document

This is the authoritative spec. Implement it **incrementally, one phase at a time**, with tests, and stop at the end of each phase for review. Do not generate the whole architecture in one pass.

**Before writing any code**, do the following and report back with a short plan:

1. Inspect this repository's layout and conventions.
2. Inspect the sibling projects that Kameleon integrates with. Their actual APIs decide the shape of the adapters; do not assume anything about them from this document:
   - Flatmate: https://github.com/gordon-matt/Flatmate (multi-tenancy)
   - Pluginz: https://github.com/gordon-matt/Pluginz (plugin discovery/loading/lifecycle)
3. Identify what Flatmate and Pluginz already provide (tenant resolution, tenant context in Blazor circuits, plugin install/load/unload, static assets) and what, if anything, would need a small generic change in them. Any change to those projects must keep them generic and must not make them depend on Kameleon.
4. Call out any constraint in Pluginz that prevents runtime loading of Razor Class Library assemblies or their static web assets.

---

## 1. Purpose

Kameleon is a theme engine delivered as Razor Class Libraries that can be added to **any** ASP.NET Core / Blazor application: a CMS, a SaaS app, a portal, a corporate site, or a single-tenant site.

A theme controls the whole presentation of a site, including layouts and page composition, not just colours and CSS. Content stays independent of the theme: switching themes never requires changing or rebuilding content.

The author's existing multi-tenant CMS is simply another Kameleon consumer. Nothing in Kameleon may reference it.

### Requirements

1. Themes are independently packaged and versioned.
2. A theme can supply completely different layouts, regions and Razor components.
3. Three delivery methods (section 3).
4. The host controls which themes are **eligible** (globally, and per tenant when multi-tenancy is in use), which is the **default**, and whether users may **override** it.
5. A user may pick a different theme only from the themes eligible in their current context.
6. Resolution is deterministic, tenant-safe, cacheable, and works with SSR, prerendering and Blazor Server circuits.
7. Single-tenant apps work with zero tenancy setup.

---

## 2. Dependency rules (hard constraints)

1. **Kameleon core has zero dependencies on Flatmate and Pluginz.**
2. `Kameleon.Flatmate` and `Kameleon.Pluginz` are optional integration packages. Either, both or neither may be used.
3. Ownership of responsibilities:

   | Concern | Owner |
   |---|---|
   | Tenant resolution and tenant context propagation (requests and Blazor circuits) | Flatmate |
   | Theme package installation, assembly loading, plugin lifecycle | Pluginz |
   | Theme discovery, eligibility, resolution, configuration, asset URLs, rendering | Kameleon |

4. Kameleon must not implement its own plugin loader or tenant resolver.
5. Flatmate and Pluginz must not depend on each other or on Kameleon.

```
Any ASP.NET Core / Blazor app (CMS, SaaS, portal, single-tenant site)
        |
   Kameleon (core)  <-- optional --  Kameleon.Flatmate  -->  Flatmate
        |             <-- optional --  Kameleon.Pluginz   -->  Pluginz
```

---

## 3. Theme types and distribution

| Type | Contents | Delivery |
|---|---|---|
| **Static theme** | `theme.json`, CSS, JS, images, fonts | A configured `Themes/` directory on disk |
| **Compiled theme (Pluginz)** | Razor layouts and components in an RCL | Installed and loaded as a Pluginz plugin |
| **Compiled theme (referenced)** | Razor layouts and components in an RCL | Project/package reference in the host, registered directly with Kameleon |

Constraints:

- Razor components are compiled code. Copying `.razor` files into `Themes/` must never be treated as executable. Component themes must be compiled assemblies.
- Do not compile files at runtime in production.
- Do not claim plugin isolation is a security boundary (section 11).

---

## 4. Theme definition

**Compiled themes**: code is the source of truth. A theme is a class implementing `ITheme` that exposes a descriptor and layouts. No separate manifest file is needed.

**Static themes**: a `theme.json` is required (below).

Keep the public SDK small, with optional capability interfaces instead of one large interface. Illustrative shapes (adapt naming to the repo):

```csharp
public interface ITheme
{
    ThemeDescriptor Descriptor { get; }
    IReadOnlyCollection<ThemeLayoutDescriptor> Layouts { get; }
}

public sealed record ThemeDescriptor(
    string Id,                 // stable, normalized: lowercase letters, digits, hyphens
    string Name,
    string Version,
    string? Description,
    ThemeKind Kind);           // Static | Component

public sealed record ThemeLayoutDescriptor(string Id, string Name, Type? ComponentType);

// Optional capabilities, implemented only when needed
public interface IThemeServiceConfigurator { void ConfigureServices(IServiceCollection services); }
public interface IThemeConfigurationProvider { ThemeConfigurationSchema GetSchema(); }
public interface IThemePageComponentProvider { IReadOnlyCollection<ThemePageComponentDescriptor> GetPageComponents(); }
```

### Static `theme.json`

```json
{
  "schemaVersion": 1,
  "id": "contoso-corporate",
  "name": "Contoso Corporate",
  "version": "1.2.0",
  "description": "Corporate site theme",
  "assets": { "stylesheet": "css/theme.css", "preview": "images/preview.png" }
}
```

A static theme has a single implicit layout (the host's default layout) plus its CSS/JS. It cannot define new layouts.

### Validation rules (apply to every theme, from any source)

- `id` is normalized and unique; never use display names as identifiers.
- Reject path traversal and absolute paths in any asset path.
- Check `schemaVersion` and SDK/host compatibility.
- Duplicate IDs are resolved deterministically (for example highest valid version wins; log the loser) and never silently.
- Treat all metadata as untrusted input. A theme cannot grant itself eligibility or permissions.
- An invalid theme is not registered; log actionable errors.

---

## 5. Theme context and resolution

### Theme context

A scoped, immutable record for the current request/circuit:

```csharp
public sealed record ThemeContext(
    string? TenantId,                  // null/"default" when not multi-tenant
    string ThemeId,
    string ThemeVersion,
    string LayoutId,
    IReadOnlyDictionary<string, object?> Configuration,
    string? UserId,
    bool IsUserOverride);
```

Never store a tenant's or user's theme in a singleton or static mutable property.

### Tenant abstraction

Core defines a minimal `IThemeTenantAccessor` (returns the current tenant ID or `null`). The default implementation is single-tenant. `Kameleon.Flatmate` provides the multi-tenant implementation backed by Flatmate.

### Resolution order

1. If the user-override policy is enabled and the user has chosen a theme that is **eligible in the current tenant**, use it.
2. Otherwise use the tenant (or app) default theme if it is installed and eligible.
3. Otherwise use the application fallback theme.

Layout is resolved the same way: tenant/app default layout, validated against the effective theme's registered layouts, falling back to the theme's default layout.

A globally installed theme is **not** automatically eligible anywhere. Every lookup is checked against the current tenant's eligibility.

### Separate responsibilities

Catalog (what is installed) → Eligibility (what may this tenant use) → Resolution (what applies now) → Rendering → Assets. Keep them as separate services.

---

## 6. Persistence

Core defines repository abstractions only (`IThemeSettingsStore`) and ships an in-memory/JSON-file default so single-tenant and sample apps work with no database. An `Kameleon.EntityFrameworkCore` package provides the EF Core implementation. Adapt to the host's existing tenant and user entities rather than duplicating them.

| Entity | Fields | Constraint |
|---|---|---|
| `TenantThemeSettings` | TenantId, DefaultThemeId, DefaultLayoutId?, AllowUserThemeOverride, UpdatedAt, UpdatedBy | one row per tenant |
| `TenantAvailableTheme` | TenantId, ThemeId, IsEnabled, Configuration (JSON)?, AddedAt | unique `(TenantId, ThemeId)` |
| `UserThemeSettings` | TenantId, UserId, ThemeId? (null = inherit), UpdatedAt | unique `(TenantId, UserId)` |

Notes:

- User preferences are per tenant. Do not store one global preference unless a consumer explicitly wants that.
- A single-tenant app uses a fixed tenant key.
- Do not rewrite persisted settings during rendering when something is invalid. Log it and provide an admin repair path.
- Layout override by users is out of scope for v1 (tenant-level only).

---

## 7. Rendering model

Page/content data is owned by the host application. Themes own only presentation.

Pipeline: resolve tenant → resolve effective theme/layout → render theme layout → render page-presentation component → render content regions.

### Layouts

- A layout is a compiled Razor component registered by `ThemeLayoutDescriptor.ComponentType`, rendered with `DynamicComponent`.
- The host passes a **typed, stable parameter model** (`ThemeLayoutModel`) containing: `ThemeContext`, the host's page view model (opaque to Kameleon via a generic/abstraction), a region renderer, a navigation model, and `IThemeAssetResolver`.
- Never pass `IServiceProvider` or tenant database services to theme components. Themes get a narrow, documented presentation API.

### Regions

Conventional region names: `Header`, `PrimaryNavigation`, `Breadcrumbs`, `Hero`, `Main`, `Sidebar`, `Aside`, `Footer`, `Overlay`. Regions are conventions, not mandatory: a theme may ignore, reorder or wrap any region. The host supplies region content through a `RegionContent` abstraction; Kameleon provides a `ThemeRegion` component to render it. Kameleon does not own any page-builder or content model.

### Optional page-type components

A theme may register presentation components for host-defined page types (Article, Landing, Search, ...). If none is registered, fall back to the generic page renderer. Themes may **not** replace host routes or auth pages unless the host explicitly opts in.

### Blazor specifics

- Resolve the theme **before** the document `<head>` is emitted (SSR/prerender) so there is no flash of the fallback theme. Provide a `ThemeHeadOutlet` that emits the correct stylesheet/script tags.
- A scoped service is not recreated per navigation in Blazor Server. When tenant/user theme settings change, notify the circuit and re-render.

---

## 8. Configuration

Merge order: theme defaults → application → tenant → user (only for settings the theme explicitly marks user-configurable). Prefer typed models or validated JSON schema; validate on save and on load; supply safe defaults. User config can never affect security, routing or tenant isolation. (User-level config can be deferred until after v1.)

---

## 9. Assets and caching

- `IThemeAssetResolver` maps a logical asset name (`images/logo.svg`) to a URL, depending on the theme source:
  - Filesystem theme → `/themes/{themeId}/{version}/{assetPath}`
  - RCL/Pluginz theme → `/_content/{PackageId}/...` (confirm how Pluginz exposes plugin static assets)
- Never hard-code asset URLs elsewhere.
- The filesystem endpoint validates theme and asset path against the registry. No raw concatenation of untrusted paths; serve only from registered roots.
- Version or hash asset URLs; immutable cache headers for versioned package assets.
- Tenant-specific mutable assets must not be served from a shared cache without tenant-safe keys.
- Do not allow manifests to inject arbitrary external script URLs. Document a CSP strategy.
- Catalog: cached, rebuilt when themes are installed, updated, disabled or removed (Pluginz events in the integration package). Tenant/user effective-theme caches are keyed by `(TenantId, UserId)` and invalidated on tenant default/eligibility/policy/user preference changes or theme removal. Never cache a user-specific result under a tenant-only key.

---

## 10. Administration

Kameleon exposes services and optional ready-made Razor admin components; the host decides where and how to authorize them.

- **Global:** list themes, versions, validation errors; enable/disable globally; set fallback theme. Install/update/remove of compiled themes is done through Pluginz's own management (not duplicated).
- **Tenant:** enable/disable themes, set default theme and layout, toggle user override, edit tenant theme configuration.
- **User:** theme selector shown only if policy allows and only listing tenant-eligible themes, plus "Use default" to clear the override.
- Preview is deferred (out of v1) unless trivially supported by the host.

---

## 11. Security

- Compiled themes are trusted code running in the host process. They are not sandboxed. Only trusted administrators may install or enable them; recommend package signing and review workflows in the docs.
- Static themes are untrusted content: validate metadata and paths, apply a script policy.
- Every theme and layout lookup is validated server-side against the current tenant's eligibility; never trust a client-supplied theme ID.
- Themes must not be able to reach another tenant's data or privileged host services.
- A tenant cannot enable a theme for another tenant.

---

## 12. Failure and fallback

The site stays usable if a theme is missing, disabled, invalid or incompatible.

| Situation | Behaviour |
|---|---|
| Invalid theme | Not registered; error logged |
| Tenant default missing or ineligible | Application fallback theme |
| User's theme no longer eligible | Ignore override; use default |
| Layout no longer exists | Theme's default layout |
| Plugin assembly missing after deploy | Fallback theme; operational error logged |
| Theme component throws | Host's normal error boundary policy; fallback where practical |

---

## 13. Solution structure

Adapt names and conventions to the repo; add projects only when they are needed.

```
src/
  Kameleon.Abstractions/            ITheme, descriptors, ThemeContext, resolver/catalog/asset
                                    interfaces, IThemeTenantAccessor, IThemeSettingsStore
  Kameleon/                         catalog, eligibility, resolver, configuration, asset resolver,
                                    in-memory/JSON store, single-tenant accessor, DI extensions
  Kameleon.FileSystem/              static theme provider, theme.json reader, asset endpoint
  Kameleon.Blazor/                  ThemeHost, ThemeLayoutRenderer, ThemeRegion,
                                    ThemeHeadOutlet, circuit refresh, optional admin components
  Kameleon.EntityFrameworkCore/     EF store and migrations
  Kameleon.Flatmate/                IThemeTenantAccessor via Flatmate        (optional)
  Kameleon.Pluginz/                 discovers ITheme from Pluginz plugins,
                                    catalog invalidation on plugin events   (optional)
samples/
  Kameleon.Sample.SingleTenant/
  Kameleon.Sample.MultiTenant/      uses both optional packages
tests/
```

`Kameleon.Pluginz` should discover `ITheme` implementations from plugin assemblies using whatever Pluginz offers today. If a small, **generic** contribution/extension-discovery mechanism in Pluginz would make this clean (plugins advertising typed contributions to other frameworks), propose it as a separate, standalone change to Pluginz after assessing what already exists. Do not build it speculatively.

---

## 14. Phases

Each phase ends with passing tests and a short summary before the next begins.

**Phase 1 — Contracts and resolution (no UI)**
Abstractions, catalog, eligibility, resolver, in-memory store, single-tenant accessor, fallback behaviour, validation rules. Tests for resolution precedence and tenant isolation.

**Phase 2 — Static filesystem themes**
`Kameleon.FileSystem`: discovery under the configured root, `theme.json` validation, safe asset endpoint, `IThemeAssetResolver`, `ThemeHeadOutlet`. Sample showing CSS/logo switching.

**Phase 3 — Compiled component themes (referenced)**
Theme SDK, `DynamicComponent` layout rendering, typed layout model, regions, page-type fallback. Sample with two themes rendering the same content with structurally different layouts.

**Phase 4 — Flatmate and Pluginz integrations**
`Kameleon.Flatmate` (tenant accessor, circuit context) and `Kameleon.Pluginz` (theme discovery, RCL static assets, install/update/remove invalidation). Multi-tenant sample. Report any proposed generic changes to Flatmate/Pluginz.

**Phase 5 — Persistence, admin and polish**
EF Core store, tenant/global admin components, user selector, circuit refresh on setting changes, caching and invalidation, diagnostics, docs.

---

## 15. Acceptance criteria

**Resolution**
- User override wins only when the policy allows it and the theme is eligible for that tenant.
- Otherwise the tenant default wins; otherwise the application fallback.
- A user in two tenants can have different preferences in each.

**Isolation**
- Tenant A cannot see or select a theme enabled only for Tenant B.
- Tenant configuration cannot leak across tenants.
- A cached resolution cannot return another user's or tenant's theme.

**Rendering**
- Two themes render the same page content with substantially different layouts.
- A theme can omit or rearrange regions.
- Unknown layout or page type falls back safely.
- Changing the theme updates the layout and the head assets, with no fallback flash on first render.

**Packaging**
- A static theme is discovered; an invalid manifest is rejected; duplicate IDs are handled deterministically.
- An RCL theme is discovered through Pluginz; plugin install/update/uninstall invalidates the catalog.
- A missing theme assembly falls back without taking the site down.

**Independence**
- Kameleon core and `Kameleon.Blazor` build and run with no reference to Flatmate or Pluginz.
- The single-tenant sample works with neither integration installed.

**Definition of done:** an administrator can enable two component themes, choose one as default, and select a layout; users can override it only when allowed and only with an eligible theme; the same content renders with different layouts; themes can come from the filesystem, Pluginz, or direct reference.

---

## 16. Do not

- Build a plugin loader, tenant system, CMS or page-builder inside Kameleon.
- Make Flatmate or Pluginz a dependency of Kameleon core.
- Restrict themes to CSS-only skinning.
- Build a theme editor or visual designer.
- Assume `.razor` files on disk can be loaded at runtime.
- Assume a globally installed theme is available to every tenant.
- Allow untrusted tenant-supplied assemblies to execute outside the host's trusted install process.
- Expose `IServiceProvider` or privileged host services to themes.
- Store the current theme in static or singleton mutable state.
