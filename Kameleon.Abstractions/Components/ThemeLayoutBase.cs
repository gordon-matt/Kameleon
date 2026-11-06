namespace Kameleon.Components;

/// <summary>
/// An optional base class for theme layouts. It receives the current <see cref="ThemeContext"/> and re-renders
/// when a region gains or loses content, so a layout can drop an empty sidebar column with <see cref="HasRegion"/>.
/// </summary>
public abstract class ThemeLayoutBase : LayoutComponentBase, IDisposable
{
    private ThemeRegionRegistry? subscribed;

    /// <summary>Never null. Outside a themed layout it is an empty context with no theme.</summary>
    [CascadingParameter]
    public ThemeContext Theme { get; set; } = ThemeContext.None(string.Empty, null);

    [CascadingParameter]
    private ThemeRegionRegistry? Regions { get; set; }

    /// <summary>
    /// Subscribes here rather than in <c>OnInitialized</c>, so a derived layout that overrides that method
    /// without calling the base still re-renders.
    /// </summary>
    public override Task SetParametersAsync(ParameterView parameters)
    {
        parameters.SetParameterProperties(this);
        if (!ReferenceEquals(subscribed, Regions))
        {
            Unsubscribe();
            subscribed = Regions;
            if (subscribed is not null)
            {
                subscribed.PresenceChanged += OnPresenceChanged;
            }
        }

        return base.SetParametersAsync(ParameterView.Empty);
    }

    /// <summary>
    /// Releases the resources used by this instance.
    /// </summary>
    public void Dispose()
    {
        Unsubscribe();
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>True when a page or the host supplies content for the region.</summary>
    protected bool HasRegion(string name) => Regions?.HasContent(name) == true;

    /// <inheritdoc cref="ThemeInfo.Asset"/>
    protected string Asset(string path) => Theme.Asset(path);

    /// <summary>
    /// Gets the setting.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <returns>The setting.</returns>
    protected string? Setting(string key) => Theme.Setting(key);

    /// <summary>
    /// Releases the resources used by this instance.
    /// </summary>
    /// <param name="disposing">The disposing.</param>
    protected virtual void Dispose(bool disposing)
    {
    }

    private void Unsubscribe()
    {
        if (subscribed is not null)
        {
            subscribed.PresenceChanged -= OnPresenceChanged;
            subscribed = null;
        }
    }

    private void OnPresenceChanged(string region) => _ = InvokeAsync(StateHasChanged);
}
