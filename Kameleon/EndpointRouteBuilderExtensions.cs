using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Net.Http.Headers;

namespace Kameleon;

/// <summary>
/// Provides extension methods for endpoint route builders.
/// </summary>
public static class EndpointRouteBuilderExtensions
{
    private static readonly FileExtensionContentTypeProvider ContentTypes = new();

    extension(IEndpointRouteBuilder endpoints)
    {
        /// <summary>
        /// Serves the assets of static themes under <see cref="KameleonOptions.AssetRequestPath"/>. Compiled
        /// themes need nothing here: their files are served with the rest of the application's static assets.
        /// </summary>
        public IEndpointConventionBuilder MapKameleon()
        {
            var options = endpoints.ServiceProvider.GetRequiredService<IOptions<KameleonOptions>>().Value;

            return endpoints
                .MapGet(options.AssetRequestPath + "/{themeId}/{version}/{**path}", ServeAsset)
                .WithTags("Kameleon")
                .ExcludeFromDescription();
        }
    }

    /// <remarks>
    /// Only files inside the folder of a theme in the catalog are served, and only types with a known content
    /// type, so a theme folder cannot be used to read anything else on the server. The version is part of the
    /// address: a request for the current version is cached for good, any other is not cached at all.
    /// </remarks>
    private static Microsoft.AspNetCore.Http.IResult ServeAsset(string themeId, string version, string? path, HttpContext context, IThemeCatalog catalog)
    {
        if (catalog.Find(themeId) is not { Kind: ThemeKind.Static } theme
            || catalog.GetContentRoot(theme.Id) is not { } root
            || ResolveFile(root, path) is not { } file
            || !ContentTypes.TryGetContentType(file, out string? contentType))
        {
            return Results.NotFound();
        }

        context.Response.Headers[HeaderNames.XContentTypeOptions] = "nosniff";
        context.Response.Headers[HeaderNames.CacheControl] = string.Equals(version, theme.Version, StringComparison.Ordinal)
            ? "public, max-age=31536000, immutable"
            : "no-cache";

        return Results.File(file, contentType, enableRangeProcessing: true);
    }

    private static string? ResolveFile(string root, string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || path.Contains('\\') || path.Contains(':'))
        {
            return null;
        }

        string[] segments = path.Split('/');
        if (segments.Any(segment => segment.Length == 0 || segment.StartsWith('.')))
        {
            return null;
        }

        string fullRoot = Path.GetFullPath(root);
        string fullPath = Path.GetFullPath(Path.Combine([fullRoot, .. segments]));
        string prefix = Path.EndsInDirectorySeparator(fullRoot) ? fullRoot : fullRoot + Path.DirectorySeparatorChar;

        return fullPath.StartsWith(prefix, OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal)
            && File.Exists(fullPath)
            && !string.Equals(Path.GetFileName(fullPath), "theme.json", StringComparison.OrdinalIgnoreCase)
                ? fullPath
                : null;
    }
}
