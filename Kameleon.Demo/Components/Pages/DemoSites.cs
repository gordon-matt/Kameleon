using Kameleon.Demo.Infrastructure;

namespace Kameleon.Demo.Components.Pages;

internal static class DemoSites
{
    public static string Current(string tenantId) =>
        DemoSiteProvider.Sites.FirstOrDefault(site => site.Id == tenantId)?.Name ?? tenantId;
}
