using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Hosting;

namespace Kameleon.FileSystem;

/// <summary>Static themes: each folder under <see cref="KameleonOptions.ThemesPath"/> that has a <c>theme.json</c>.</summary>
internal sealed class FileSystemThemeSource(IHostEnvironment environment, IOptions<KameleonOptions> options) : IThemeSource
{
    public const string ManifestFileName = "theme.json";

    /// <summary>A manifest is a few hundred bytes; anything this large is not one and is not read into memory.</summary>
    private const long MaxManifestBytes = 64 * 1024;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        MaxDepth = 16,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: false) },
    };

    public ThemeSourceResult Load()
    {
        string? configured = options.Value.ThemesPath;
        if (string.IsNullOrWhiteSpace(configured))
        {
            return ThemeSourceResult.Empty;
        }

        string root = Path.GetFullPath(Path.Combine(environment.ContentRootPath, configured));
        if (!Directory.Exists(root))
        {
            return ThemeSourceResult.Empty;
        }

        string requestPath = options.Value.AssetRequestPath.Trim('/');
        var candidates = new List<ThemeCandidate>();
        var errors = new List<ThemeCatalogError>();

        foreach (string folder in Directory.EnumerateDirectories(root).Order(StringComparer.Ordinal))
        {
            // Shown on the admin screens, so relative: the server's own folder layout is nobody's business.
            string source = Path.GetRelativePath(environment.ContentRootPath, folder).Replace('\\', '/');
            var manifestFile = new FileInfo(Path.Combine(folder, ManifestFileName));
            if (!manifestFile.Exists)
            {
                continue;
            }

            if (manifestFile.Length > MaxManifestBytes)
            {
                errors.Add(new ThemeCatalogError(source, null, $"{ManifestFileName} is larger than {MaxManifestBytes / 1024} KB."));
                continue;
            }

            ThemeManifest? manifest;
            try
            {
                using var stream = manifestFile.OpenRead();
                manifest = JsonSerializer.Deserialize<ThemeManifest>(stream, JsonOptions);
            }
            catch (Exception exception) when (exception is JsonException or IOException or UnauthorizedAccessException)
            {
                errors.Add(new ThemeCatalogError(source, null, $"{ManifestFileName} could not be read: {exception.Message}"));
                continue;
            }

            if (manifest is null)
            {
                errors.Add(new ThemeCatalogError(source, null, $"{ManifestFileName} is empty."));
                continue;
            }

            string id = string.IsNullOrWhiteSpace(manifest.Id) ? Path.GetFileName(folder).ToLowerInvariant() : manifest.Id;
            if (manifest.SchemaVersion != ThemeManifest.CurrentSchemaVersion)
            {
                errors.Add(new ThemeCatalogError(source, id,
                    $"schemaVersion {manifest.SchemaVersion} is not supported. This version of Kameleon reads schemaVersion {ThemeManifest.CurrentSchemaVersion}."));
                continue;
            }

            string version = string.IsNullOrWhiteSpace(manifest.Version) ? "1.0.0" : manifest.Version;
            candidates.Add(new ThemeCandidate
            {
                Source = source,
                Kind = ThemeKind.Static,
                Descriptor = new ThemeDescriptor
                {
                    Id = id,
                    Name = string.IsNullOrWhiteSpace(manifest.Name) ? id : manifest.Name,
                    Version = version,
                    Description = manifest.Description,
                    Author = manifest.Author,
                    PreviewImage = manifest.Preview,
                    Stylesheets = manifest.Stylesheets ?? [],
                    Scripts = manifest.Scripts ?? [],
                    SupportsRightToLeft = manifest.SupportsRightToLeft,
                },
                Settings = [.. (manifest.Settings ?? []).Select(setting => setting.ToDefinition())],
                AssetBasePath = $"{requestPath}/{id}/{version}/",
                ContentRoot = folder,
            });
        }

        return new ThemeSourceResult(candidates, errors);
    }
}
