using Kameleon.Tenancy;

namespace Kameleon.Demo.Infrastructure;

/// <summary>
/// Two pretend sites in one application, picked with a cookie. A real host takes the tenant from its own
/// tenancy instead: the host name, a path prefix or the signed-in user's organisation.
/// </summary>
public sealed class DemoSiteProvider(IHttpContextAccessor httpContextAccessor) : IThemeTenantProvider
{
    public const string CookieName = "kameleon-demo.site";

    public static readonly IReadOnlyList<ThemeTenant> Sites =
    [
        new("main", "Main site"),
        new("north", "North branch"),
    ];

    public string GetTenantId()
    {
        string? requested = httpContextAccessor.HttpContext?.Request.Cookies[CookieName];
        return Sites.Any(site => site.Id == requested) ? requested! : Sites[0].Id;
    }

    public sealed class Directory : IThemeTenantDirectory
    {
        public Task<IReadOnlyList<ThemeTenant>> ListAsync(CancellationToken cancellationToken = default) => Task.FromResult(Sites);
    }
}

public static class DemoSiteEndpoints
{
    extension(IEndpointRouteBuilder endpoints)
    {
        /// <summary>A full page load is needed anyway, because the theme's stylesheets are in the document head.</summary>
        public void MapDemoSites() => endpoints.MapGet("/demo/site/{id}", (string id, HttpContext context) =>
        {
            if (DemoSiteProvider.Sites.Any(site => site.Id == id))
            {
                context.Response.Cookies.Append(DemoSiteProvider.CookieName, id, new CookieOptions { HttpOnly = true, SameSite = SameSiteMode.Lax, IsEssential = true });
            }

            return Results.LocalRedirect("~/");
        });
    }
}
