namespace Kameleon.Components;

/// <summary>
/// The layout of themed pages: set it as the router's default layout or with <c>@layout</c>. It renders the
/// resolved theme's layout around the page, or <see cref="KameleonOptions.HostLayout"/> for a static theme and
/// when no theme resolves, and passes the theme and the page's region content down to it.
/// </summary>
public partial class KameleonLayout
{
    private ThemeContext? context;
    private ThemeRegionRegistry regions = default!;

    [Inject]
    private ThemeState State { get; set; } = default!;

    [Inject]
    private IOptions<KameleonOptions> Options { get; set; } = default!;

    private Type LayoutType =>
        context?.Layout?.ComponentType ?? Options.Value.HostLayout ?? typeof(KameleonDefaultLayout);

    /// <summary>
    /// Releases the resources used by this instance.
    /// </summary>
    public void Dispose() => State.Changed -= OnThemeChanged;

    /// <inheritdoc/>
    protected override async Task OnInitializedAsync()
    {
        regions = new ThemeRegionRegistry(Options.Value.RegionContent.ToDictionary(
            entry => entry.Key,
            entry => (IReadOnlyList<Type>)[.. entry.Value],
            StringComparer.Ordinal));

        State.Changed += OnThemeChanged;
        context = await State.GetAsync();
    }

    private void OnThemeChanged(ThemeContext resolved) => _ = InvokeAsync(() =>
    {
        context = resolved;
        StateHasChanged();
    });
}
