namespace Kameleon.Components;

/// <summary>
/// Connects <see cref="RegionContent"/> in a page with the <see cref="ThemeRegion"/> that shows it. The themed
/// layout creates one and cascades it, so each request or circuit has its own.
/// </summary>
/// <remarks>
/// Not built on Blazor sections: a section shows only the most recently added content, and the order in which
/// a layout's regions and a page's content are added depends on where the region sits relative to <c>@Body</c>.
/// Here a page's content always wins over the host's defaults, wherever the region is placed.
/// </remarks>
public sealed class ThemeRegionRegistry(IReadOnlyDictionary<string, IReadOnlyList<Type>>? hostContent = null)
{
    private readonly Lock sync = new();
    private readonly List<Entry> entries = [];
    private readonly IReadOnlyDictionary<string, IReadOnlyList<Type>> hostContent =
        hostContent ?? ReadOnlyDictionary<string, IReadOnlyList<Type>>.Empty;

    /// <summary>Raised with the region name whenever a page's content for it is added, changed or removed.</summary>
    public event Action<string>? ContentChanged;

    /// <summary>
    /// Raised only when a region goes from empty to filled or back. A layout listens to this rather than to
    /// <see cref="ContentChanged"/>, because re-rendering the layout on every content change would render the
    /// page again and loop.
    /// </summary>
    public event Action<string>? PresenceChanged;

    /// <summary>What pages supplied for the region, in the order they were added.</summary>
    public IReadOnlyList<RenderFragment> GetPageContent(string region)
    {
        lock (sync)
        {
            return [.. entries.Where(entry => entry.Region == region).Select(entry => entry.Fragment)];
        }
    }

    /// <summary>Components the host renders in the region when no page supplies content for it.</summary>
    public IReadOnlyList<Type> GetHostContent(string region) => hostContent.GetValueOrDefault(region) ?? [];

    public bool HasContent(string region) => HasPageContent(region) || GetHostContent(region).Count > 0;

    internal void Set(object owner, string region, RenderFragment? fragment)
    {
        if (fragment is null || string.IsNullOrEmpty(region))
        {
            Remove(owner);
            return;
        }

        string? previousRegion;
        bool hadPrevious;
        bool hadNew;
        lock (sync)
        {
            int index = entries.FindIndex(entry => ReferenceEquals(entry.Owner, owner));
            previousRegion = index >= 0 ? entries[index].Region : null;
            hadPrevious = previousRegion is not null;
            hadNew = entries.Any(entry => entry.Region == region);

            if (index >= 0 && previousRegion == region)
            {
                entries[index] = entries[index] with { Fragment = fragment };
            }
            else
            {
                if (index >= 0)
                {
                    entries.RemoveAt(index);
                }

                entries.Add(new Entry(owner, region, fragment));
            }
        }

        if (hadPrevious && previousRegion != region)
        {
            Raise(previousRegion!, presenceFlipped: !HasPageContent(previousRegion!));
        }

        Raise(region, presenceFlipped: !hadNew);
    }

    internal void Remove(object owner)
    {
        string? region;
        lock (sync)
        {
            int index = entries.FindIndex(entry => ReferenceEquals(entry.Owner, owner));
            if (index < 0)
            {
                return;
            }

            region = entries[index].Region;
            entries.RemoveAt(index);
        }

        Raise(region, presenceFlipped: !HasPageContent(region));
    }

    private bool HasPageContent(string region)
    {
        lock (sync)
        {
            return entries.Any(entry => entry.Region == region);
        }
    }

    private void Raise(string region, bool presenceFlipped)
    {
        ContentChanged?.Invoke(region);
        if (presenceFlipped)
        {
            PresenceChanged?.Invoke(region);
        }
    }

    private sealed record Entry(object Owner, string Region, RenderFragment Fragment);
}
