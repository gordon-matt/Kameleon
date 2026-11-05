using System.Collections.ObjectModel;

namespace Kameleon.Catalog;

/// <summary>
/// Built on first use and replaced as a whole on <see cref="Reload"/>, so a reader never sees half a catalog.
/// </summary>
internal sealed class ThemeCatalog(
    IEnumerable<IThemeSource> sources,
    IOptions<KameleonOptions> options,
    ILogger<ThemeCatalog> logger) : IThemeCatalog
{
    private readonly Lock sync = new();
    private Snapshot? snapshot;

    public IReadOnlyList<ThemeInfo> Themes => Current.Themes;

    public IReadOnlyList<ThemeCatalogError> Errors => Current.Errors;

    private Snapshot Current
    {
        get
        {
            var current = Volatile.Read(ref snapshot);
            if (current is not null)
            {
                return current;
            }

            lock (sync)
            {
                return snapshot ??= Build();
            }
        }
    }

    public ThemeInfo? Find(string? themeId) =>
        themeId is not null && Current.ById.TryGetValue(themeId, out var theme) ? theme : null;

    public string? GetContentRoot(string themeId) => Current.ContentRoots.GetValueOrDefault(themeId);

    public void Reload()
    {
        var built = Build();
        lock (sync)
        {
            snapshot = built;
        }
    }

    private Snapshot Build()
    {
        var candidates = new List<ThemeCandidate>();
        var errors = new List<ThemeCatalogError>();

        foreach (var source in sources)
        {
            try
            {
                var result = source.Load();
                candidates.AddRange(result.Themes);
                errors.AddRange(result.Errors);
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "The theme source {ThemeSource} failed", source.GetType().Name);
                errors.Add(new ThemeCatalogError(source.GetType().Name, null, $"The source failed: {exception.Message}"));
            }
        }

        var valid = new List<(ThemeCandidate Candidate, ThemeVersion Version)>();
        foreach (var candidate in candidates)
        {
            var problems = ThemeValidator.Validate(candidate, options.Value);
            if (problems.Count == 0 && ThemeVersion.TryParse(candidate.Descriptor.Version, out var version))
            {
                valid.Add((candidate, version));
                continue;
            }

            foreach (string problem in problems)
            {
                logger.LogError("The theme {ThemeId} from {ThemeSource} was not registered: {Problem}", candidate.Descriptor.Id, candidate.Source, problem);
                errors.Add(new ThemeCatalogError(candidate.Source, candidate.Descriptor.Id, problem));
            }
        }

        var themes = new List<ThemeInfo>();
        var contentRoots = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var group in valid.GroupBy(item => item.Candidate.Descriptor.Id, StringComparer.Ordinal))
        {
            // Highest version, then compiled over static, then source name, so the same files always give the same winner.
            var ranked = group
                .OrderByDescending(item => item.Version)
                .ThenBy(item => item.Candidate.Kind == ThemeKind.Compiled ? 0 : 1)
                .ThenBy(item => item.Candidate.Source, StringComparer.Ordinal)
                .ToList();

            var winner = ranked[0].Candidate;
            foreach (var (loser, _) in ranked.Skip(1))
            {
                string message = $"Another theme with this id was registered instead: version {winner.Descriptor.Version} from {winner.Source}.";
                logger.LogWarning("The theme {ThemeId} {ThemeVersion} from {ThemeSource} was not registered. {Reason}", loser.Descriptor.Id, loser.Descriptor.Version, loser.Source, message);
                errors.Add(new ThemeCatalogError(loser.Source, loser.Descriptor.Id, message));
            }

            themes.Add(ToInfo(winner));
            if (winner.ContentRoot is not null)
            {
                contentRoots[winner.Descriptor.Id] = winner.ContentRoot;
            }
        }

        themes.Sort((left, right) => StringComparer.OrdinalIgnoreCase.Compare(left.Name, right.Name));
        logger.LogInformation("Kameleon registered {ThemeCount} theme(s) and rejected {ErrorCount}", themes.Count, errors.Count);

        return new Snapshot(
            themes,
            themes.ToDictionary(theme => theme.Id, StringComparer.Ordinal),
            contentRoots,
            errors);
    }

    private static ThemeInfo ToInfo(ThemeCandidate candidate)
    {
        var descriptor = candidate.Descriptor;
        var layouts = candidate.Layouts
            .Select(layout => new ThemeLayoutInfo(layout.Id, layout.Name, layout.Description, layout.ComponentType))
            .ToList();

        var info = new ThemeInfo
        {
            Id = descriptor.Id,
            Name = descriptor.Name,
            Version = descriptor.Version,
            Description = descriptor.Description,
            Author = descriptor.Author,
            Kind = candidate.Kind,
            AssetBasePath = candidate.AssetBasePath,
            Layouts = layouts,
            Views = new ReadOnlyDictionary<string, Type>(candidate.Views.ToDictionary(StringComparer.Ordinal)),
            Settings = [.. candidate.Settings],
            SupportsRightToLeft = descriptor.SupportsRightToLeft,
            Source = candidate.Source,
        };

        return info with
        {
            DefaultLayout = info.FindLayout(descriptor.DefaultLayoutId) ?? layouts.FirstOrDefault(),
            StylesheetUrls = [.. (descriptor.Stylesheets ?? []).Select(info.Asset)],
            ScriptUrls = [.. (descriptor.Scripts ?? []).Select(info.Asset)],
            PreviewImageUrl = descriptor.PreviewImage is null ? null : info.Asset(descriptor.PreviewImage),
        };
    }

    private sealed record Snapshot(
        IReadOnlyList<ThemeInfo> Themes,
        IReadOnlyDictionary<string, ThemeInfo> ById,
        IReadOnlyDictionary<string, string> ContentRoots,
        IReadOnlyList<ThemeCatalogError> Errors);
}
